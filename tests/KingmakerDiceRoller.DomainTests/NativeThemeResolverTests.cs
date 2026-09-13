using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerDiceRoller.UI;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class NativeThemeResolverTests
    {
        internal static void LiteralSlashChildUsesProductionResolver()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            NativeThemeResolution result = fixture.Resolve();
            AssertEx.Equal(NativeThemeStatus.FullyThemed, result.Status);
            AssertEx.True(ReferenceEquals(fixture.Label.Values[0], result.Get(NativeThemeCapability.ButtonText).Components[0]));
            AssertEx.True(result.Get(NativeThemeCapability.ButtonText).Identity.Contains("literal child 'Next/Complete text'"));
        }
        internal static void LiteralAndPathRemainDistinct()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            var next = fixture.Action.Add("Next");
            var nested = next.Add("Complete text", NativeThemeComponent.Text);
            var lookup = new NativeUiDonorLookup(fixture);
            AssertEx.True(ReferenceEquals(nested, lookup.RequirePath(fixture.Action, "Next/Complete text")));
            AssertEx.True(ReferenceEquals(fixture.Label, lookup.RequireLiteralChild(fixture.Action, "Next/Complete text")));
            AssertEx.True(ReferenceEquals(fixture.Label.Values[0], fixture.Resolve().Get(NativeThemeCapability.ButtonText).Components[0]));
            fixture.Action.Children.Remove(fixture.Label);
            NativeThemeResolution result = fixture.Resolve();
            AssertEx.True(result.Get(NativeThemeCapability.ButtonText) == null);
            AssertEx.True(result.Get(NativeThemeCapability.Buttons) != null);
            AssertEx.True(result.Failure(NativeThemeCapability.ButtonText).Contains("child not found"));
        }
        internal static void LiteralDoesNotSatisfyHierarchyPath()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            string error = AssertEx.Throws<InvalidOperationException>(() =>
                new NativeUiDonorLookup(fixture).RequirePath(fixture.Action, "Next/Complete text")).Message;
            AssertEx.True(error.Contains("locator=path 'Next/Complete text'"));
            AssertEx.True(error.Contains("child not found: 'Next'"));
        }
        internal static void InactiveDonorsRemainEligible()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            foreach (ThemeFixture.Node node in fixture.Nodes.Values) node.Active = false;
            fixture.Root.Active = fixture.Label.Active = false;
            AssertEx.Equal(NativeThemeStatus.FullyThemed, fixture.Resolve().Status);
        }
        internal static void LiteralMatchIsOrdinalAndRejectsDuplicates()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            fixture.Action.Add("next/complete text", NativeThemeComponent.Text);
            AssertEx.Equal(NativeThemeStatus.FullyThemed, fixture.Resolve().Status);
            fixture.Action.Add("Next/Complete text", NativeThemeComponent.Text);
            NativeThemeResolution result = fixture.Resolve();
            AssertEx.Equal(NativeThemeStatus.PartiallyThemed, result.Status);
            AssertEx.True(result.Failure(NativeThemeCapability.ButtonText).Contains("ambiguous direct children"));
            AssertEx.True(result.Get(NativeThemeCapability.Buttons) != null);
        }
        internal static void WrongComponentDoesNotSelectUnrelatedText()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            fixture.Label.Values.Clear();
            fixture.Label.Values.Add(new ThemeFixture.Value(NativeThemeComponent.Image));
            fixture.Action.Add("Other text", NativeThemeComponent.Text);
            string error = fixture.Resolve().Failure(NativeThemeCapability.ButtonText);
            AssertEx.True(error.Contains("expected component missing: Text"));
            AssertEx.True(error.Contains("literal child 'Next/Complete text'"));
            AssertEx.True(error.Contains("owner=") && error.Contains("'BackButton'") && error.Contains("direct children="));
        }
        internal static void DuplicateComponentsFailCapability()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            fixture.Label.Values.Add(new ThemeFixture.Value(NativeThemeComponent.Text));
            AssertEx.True(fixture.Resolve().Failure(NativeThemeCapability.ButtonText).Contains("ambiguous expected components"));
        }
        internal static void MissingOwnerAndDestroyedComponentsFailClearly()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            NativeThemeResolution missing = NativeThemeResolver.Resolve(null, fixture);
            AssertEx.Equal(NativeThemeStatus.Fallback, missing.Status);
            AssertEx.True(missing.Failure(NativeThemeCapability.Paper).Contains("owner missing/destroyed"));
            fixture.Label.Values[0].Alive = false;
            AssertEx.True(fixture.Resolve().Failure(NativeThemeCapability.ButtonText).Contains("expected component destroyed"));
        }
        internal static void AmbiguousPathDoesNotChooseFirstDonor()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            fixture.Action.Parent.Add("BackButton", NativeThemeComponent.Button);
            NativeThemeResolution result = fixture.Resolve();
            AssertEx.True(result.Failure(NativeThemeCapability.Buttons).Contains("ambiguous direct children"));
            AssertEx.True(result.Get(NativeThemeCapability.Paper) != null);
        }
        internal static void OptionalCapabilitiesFailIndependently()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            fixture.Remove(NativeThemeResolver.InputPath + "/Frame");
            fixture.Remove(NativeThemeResolver.ScrollPath + "/Sliding Area/Handle");
            fixture.Remove(NativeThemeResolver.RulePath);
            NativeThemeResolution result = fixture.Resolve();
            AssertEx.Equal(6, result.AvailableCount);
            AssertEx.Equal(NativeThemeStatus.PartiallyThemed, result.Status);
            foreach (NativeThemeCapability c in new[] { NativeThemeCapability.Paper, NativeThemeCapability.Buttons,
                NativeThemeCapability.Heading, NativeThemeCapability.Body, NativeThemeCapability.ButtonText, NativeThemeCapability.Selector })
                AssertEx.True(result.Get(c) != null, c.ToString());
            foreach (NativeThemeCapability c in new[] { NativeThemeCapability.Input, NativeThemeCapability.Scrollbar, NativeThemeCapability.Ornament })
                AssertEx.True(result.Get(c) == null && result.Failure(c).Contains("child not found"), c.ToString());
        }
        internal static void FailedValidationNeverCommitsPartialCapability()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            fixture.Nodes[NativeThemeResolver.InputPath + "/Frame"].Values[0].Valid = false;
            NativeThemeResolution result = fixture.Resolve();
            AssertEx.True(result.Get(NativeThemeCapability.Input) == null);
            AssertEx.Equal(8, result.AvailableCount);
            AssertEx.True(result.Failure(NativeThemeCapability.Input).Contains("validation rejected") &&
                result.Failure(NativeThemeCapability.Input).Contains("locator=path") && result.Failure(NativeThemeCapability.Input).Contains("owner="));
        }
        internal static void MissingCriticalCapabilityCannotClaimFullyThemed()
        {
            foreach (string path in new[] { NativeThemeResolver.PaperPath, NativeThemeResolver.ActionPath, NativeThemeResolver.BodyPath })
            {
                ThemeFixture fixture = ThemeFixture.Full();
                fixture.Remove(path);
                AssertEx.Equal(NativeThemeStatus.PartiallyThemed, fixture.Resolve().Status);
            }
            AssertEx.Equal(NativeThemeStatus.Fallback, new ThemeFixture().Resolve().Status);
        }
        internal static void LookupAndDiagnosticsAreBounded()
        {
            var fixture = new ThemeFixture();
            for (int index = 0; index <= NativeUiDonorLookup.MaximumChildren; index++)
                fixture.Root.Add("child" + index + new string('x', 400));
            string message = AssertEx.Throws<InvalidOperationException>(() =>
                new NativeUiDonorLookup(fixture).RequireLiteralChild(fixture.Root, "missing")).Message;
            AssertEx.True(message.Contains("count exceeds") && message.Contains("locator=literal child 'missing'"));
            AssertEx.True(message.Length < 1800, "diagnostic must not dump the hierarchy");
            AssertEx.True(fixture.ChildReads <= 6, "over-limit search must not scan all children");
        }
        internal static void RecoveryWaitsForFillAndStopsAtLimit()
        {
            var recovery = new NativeThemeRecovery();
            object allocator = new object(), owner = new object();
            recovery.Bind(allocator, owner);
            AssertEx.True(recovery.TryBegin(true, false));
            AssertEx.True(!recovery.TryBegin(true, true), "nested FillData must not reenter");
            recovery.Complete();
            for (int index = 0; index < 100; index++) AssertEx.True(!recovery.TryBegin(true, false));
            for (int index = 1; index < NativeThemeRecovery.MaximumAttempts; index++)
            {
                recovery.Bind(allocator, owner);
                AssertEx.True(recovery.TryBegin(true, true));
                recovery.Complete();
            }
            for (int index = 0; index < 100; index++)
            {
                recovery.Bind(allocator, owner);
                AssertEx.True(!recovery.TryBegin(true, true));
            }
            AssertEx.Equal(3, recovery.Attempts);
        }
        internal static void LateDonorsRestyleExistingBindingsOnce()
        {
            var fixture = new ThemeFixture();
            var recovery = new NativeThemeRecovery();
            var bindings = new NativeThemeBindings();
            int applied = 0, fallback = 0;
            bindings.Add(NativeThemeCapability.Buttons, values => applied++, () => fallback++);
            recovery.Bind(new object(), fixture.Root);
            AssertEx.True(recovery.TryBegin(true, false));
            NativeThemeResolution result = fixture.Resolve();
            bindings.Apply(result, message => { throw new Exception(message); });
            recovery.Complete();
            fixture.Populate();
            AssertEx.True(recovery.TryBegin(true, true));
            result = fixture.Resolve();
            bindings.Apply(result, message => { throw new Exception(message); });
            recovery.Complete();
            AssertEx.Equal(NativeThemeStatus.FullyThemed, result.Status);
            AssertEx.True(!recovery.TryBegin(false, true));
            AssertEx.Equal(1, applied);
            AssertEx.Equal(1, fallback);
            AssertEx.Equal(1, bindings.Count);
        }
        internal static void UnchangedButtonsAreNotRestyledByOptionalRetries()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            fixture.Remove(NativeThemeResolver.RulePath);
            var bindings = new NativeThemeBindings();
            int buttonStyles = 0;
            bindings.Add(NativeThemeCapability.Buttons, values => buttonStyles++, () => { });
            for (int index = 0; index < 3; index++) bindings.Apply(fixture.Resolve(), message => { throw new Exception(message); });
            AssertEx.Equal(1, buttonStyles, "unrelated retries must not reset a held button's presentation");
        }
        internal static void DestroyedAndMovedDonorsDiscardOnlyTheirCapability()
        {
            ThemeFixture fixture = ThemeFixture.Full();
            NativeThemeResolution result = fixture.Resolve();
            fixture.Nodes[NativeThemeResolver.PaperPath].Alive = false;
            fixture.Label.Parent = new ThemeFixture.Node("unrelated owner");
            AssertEx.True(result.DiscardStale());
            AssertEx.True(result.Get(NativeThemeCapability.Paper) == null && result.Get(NativeThemeCapability.ButtonText) == null);
            AssertEx.True(result.Get(NativeThemeCapability.Buttons) != null && result.Get(NativeThemeCapability.Body) != null);
            AssertEx.True(!result.DiscardStale(), "stale diagnostics must not repeat per frame");
            fixture.Root.Alive = false;
            AssertEx.True(result.DiscardStale());
            AssertEx.Equal(NativeThemeStatus.Fallback, result.Status);
        }
        internal static void NewAllocatorOrHierarchyResetsRecoveryIdentity()
        {
            var recovery = new NativeThemeRecovery();
            object allocator = new object(), owner = new object();
            recovery.Bind(allocator, owner);
            for (int index = 0; index < 3; index++) { AssertEx.True(recovery.TryBegin(true, true)); recovery.Complete(); }
            recovery.Bind(new object(), owner);
            AssertEx.Equal(0, recovery.Attempts);
            AssertEx.True(recovery.TryBegin(true, false)); recovery.Complete();
            recovery.Bind(allocator, new object());
            AssertEx.Equal(0, recovery.Attempts);
            recovery.Reset();
            AssertEx.Equal(0, recovery.Attempts);
        }
        internal static void CapabilityApplicationFailureRestoresItsWholeGroup()
        {
            NativeThemeResolution result = ThemeFixture.Full().Resolve();
            var bindings = new NativeThemeBindings();
            bool firstPaper = false, secondPaper = false, button = false;
            bindings.Add(NativeThemeCapability.Paper, v => firstPaper = true, () => firstPaper = false);
            bindings.Add(NativeThemeCapability.Paper, v => { secondPaper = true; throw new InvalidOperationException("fixture style failure"); }, () => secondPaper = false);
            bindings.Add(NativeThemeCapability.Buttons, v => button = true, () => button = false);
            bindings.Apply(result, message => { throw new Exception(message); });
            AssertEx.True(!firstPaper && !secondPaper && button);
            AssertEx.Equal(NativeThemeStatus.PartiallyThemed, result.Status);
            AssertEx.True(result.Failure(NativeThemeCapability.Paper).Contains("fixture style failure"));
        }
        internal static void TeardownClearsOwnedBindingsAndFallbackFailureIsContained()
        {
            var bindings = new NativeThemeBindings();
            int diagnostics = 0, styles = 0;
            bindings.Add(NativeThemeCapability.Paper, v => styles++, () => { throw new InvalidOperationException("owned fallback fixture"); });
            bindings.Apply(new ThemeFixture().Resolve(), message => diagnostics++);
            AssertEx.Equal(1, diagnostics);
            bindings.Clear();
            bindings.Apply(ThemeFixture.Full().Resolve(), message => diagnostics++);
            AssertEx.Equal(0, styles);
            AssertEx.Equal(0, bindings.Count);
        }
    }

    // Data fixture only. All lookup, capability grouping, error isolation and
    // recovery execute production code linked into this deterministic runner.
    // Unity object lifetime/rendering and sprite/font validation need the native harness.
    internal sealed class ThemeFixture : INativeThemeSource
    {
        internal sealed class Value
        {
            internal NativeThemeComponent Kind;
            internal bool Alive = true, Valid = true;
            internal Value(NativeThemeComponent kind) { Kind = kind; }
        }
        internal sealed class Node
        {
            internal readonly string Name;
            internal Node Parent;
            internal bool Alive = true, Active = true;
            internal readonly List<Node> Children = new List<Node>();
            internal readonly List<Value> Values = new List<Value>();
            internal Node(string name) { Name = name; }
            internal Node Add(string name, NativeThemeComponent? kind = null)
            {
                var node = new Node(name) { Parent = this };
                if (kind.HasValue) node.Values.Add(new Value(kind.Value));
                Children.Add(node);
                return node;
            }
        }
        internal readonly Node Root = new Node("CharacterBuild");
        internal readonly Dictionary<string, Node> Nodes = new Dictionary<string, Node>();
        internal Node Action, Label;
        internal int ChildReads;
        internal static ThemeFixture Full() { var fixture = new ThemeFixture(); fixture.Populate(); return fixture; }
        internal void Populate()
        {
            AddPath(NativeThemeResolver.PaperPath, NativeThemeComponent.Image);
            Action = AddPath(NativeThemeResolver.ActionPath, NativeThemeComponent.Button);
            Label = Action.Add("Next/Complete text", NativeThemeComponent.Text);
            AddPath(NativeThemeResolver.HeadingPath, NativeThemeComponent.Text);
            AddPath(NativeThemeResolver.BodyPath, NativeThemeComponent.Text);
            AddPath(NativeThemeResolver.SelectorPath, NativeThemeComponent.Text);
            AddPath(NativeThemeResolver.InputPath, NativeThemeComponent.Image);
            AddPath(NativeThemeResolver.InputPath + "/Frame", NativeThemeComponent.Image);
            AddPath(NativeThemeResolver.InputPath + "/InputField", NativeThemeComponent.Input);
            AddPath(NativeThemeResolver.ScrollPath, NativeThemeComponent.Image).Values.Add(new Value(NativeThemeComponent.Scrollbar));
            AddPath(NativeThemeResolver.ScrollPath + "/Sliding Area/Handle", NativeThemeComponent.Image);
            AddPath(NativeThemeResolver.RulePath, NativeThemeComponent.Image);
        }
        private Node AddPath(string path, NativeThemeComponent kind)
        {
            Node node = Root;
            foreach (string part in path.Split('/')) node = node.Children.FirstOrDefault(n => n.Name == part) ?? node.Add(part);
            node.Values.Add(new Value(kind));
            Nodes[path] = node;
            return node;
        }
        internal void Remove(string path) { Node node = Nodes[path]; node.Parent.Children.Remove(node); node.Parent = null; }
        internal NativeThemeResolution Resolve() { return NativeThemeResolver.Resolve(Root, this); }
        public bool SameNode(object first, object second) { return ReferenceEquals(first, second); }
        public bool IsAlive(object value) { return value is Node ? ((Node)value).Alive : value is Value && ((Value)value).Alive; }
        public string Name(object node) { return ((Node)node).Name; }
        public object Parent(object node) { return ((Node)node).Parent; }
        public int ChildCount(object node) { return ((Node)node).Children.Count; }
        public object Child(object node, int index) { ChildReads++; return ((Node)node).Children[index]; }
        public object[] Components(object node, NativeThemeComponent component) { return ((Node)node).Values.Where(v => v.Kind == component).Cast<object>().ToArray(); }
        public void Validate(NativeThemeCapability capability, object[] components)
        {
            if (components.Cast<Value>().Any(v => !v.Valid)) throw new InvalidOperationException("fixture validation rejected " + capability);
        }
    }
}
