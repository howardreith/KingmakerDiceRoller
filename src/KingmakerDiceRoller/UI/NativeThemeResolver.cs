using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.UI
{
    internal enum NativeThemeCapability { Paper, Buttons, Heading, Body, ButtonText, Selector, Input, Scrollbar, Ornament }
    internal enum NativeThemeStatus { Fallback, PartiallyThemed, FullyThemed }

    internal sealed class NativeThemeResource
    {
        internal object[] Nodes;
        internal object[] Components;
        internal string Identity;
    }

    internal sealed class NativeThemeResolution
    {
        internal static readonly NativeThemeCapability[] Capabilities = (NativeThemeCapability[])Enum.GetValues(typeof(NativeThemeCapability));
        private readonly Dictionary<NativeThemeCapability, NativeThemeResource> resources = new Dictionary<NativeThemeCapability, NativeThemeResource>();
        private readonly Dictionary<NativeThemeCapability, string> failures = new Dictionary<NativeThemeCapability, string>();
        private readonly INativeThemeSource source;
        internal readonly object Owner;

        internal NativeThemeResolution(object owner, INativeThemeSource source) { Owner = owner; this.source = source; }
        internal NativeThemeStatus Status => resources.Count == Capabilities.Length ? NativeThemeStatus.FullyThemed :
            resources.Count == 0 ? NativeThemeStatus.Fallback : NativeThemeStatus.PartiallyThemed;
        internal int AvailableCount => resources.Count;
        internal NativeThemeResource Get(NativeThemeCapability capability)
        {
            NativeThemeResource value;
            return resources.TryGetValue(capability, out value) ? value : null;
        }
        internal string Failure(NativeThemeCapability capability)
        {
            string value;
            return failures.TryGetValue(capability, out value) ? value : null;
        }
        internal void Accept(NativeThemeCapability capability, NativeThemeResource resource)
        {
            resources[capability] = resource;
            failures.Remove(capability);
        }
        internal void Reject(NativeThemeCapability capability, string reason)
        {
            resources.Remove(capability);
            failures[capability] = reason;
        }
        internal bool DiscardStale()
        {
            bool changed = false;
            var lookup = new NativeUiDonorLookup(source);
            foreach (NativeThemeCapability capability in Capabilities)
            {
                NativeThemeResource resource = Get(capability);
                if (resource == null) continue;
                bool alive = source.IsAlive(Owner);
                foreach (object node in resource.Nodes) alive &= lookup.IsUnderOwner(node, Owner);
                foreach (object component in resource.Components) alive &= source.IsAlive(component);
                if (!alive)
                {
                    Reject(capability, "cached donor destroyed or moved outside its owner; " + resource.Identity);
                    changed = true;
                }
            }
            return changed;
        }
        internal string Summary => "status=" + Status + "; capabilities=" + AvailableCount + "/" + Capabilities.Length;
    }

    // This is the production resolver, including the exact locators and capability
    // grouping. Tests supply only node/component access and validation adapters.
    internal static class NativeThemeResolver
    {
        internal const string PaperPath = "Body/Content/ClothColorSelector/PrimarySelectorPlace/ColorSelector/Background";
        internal const string ActionPath = "Body/Bottom (1)/ButtonsPlace/BackButton";
        internal const string ButtonLabelName = "Next/Complete text";
        internal const string HeadingPath = "Body/Content/SkillsMiddleScoresAllocator/Content/STR Background/Labels/SHORT";
        internal const string BodyPath = "Body/Content/Book/Image_Book/Container_SpellsLeft/Spells_Container/SpellBookItem (2)/Item/Body/NamePlace/LabelName";
        internal const string SelectorPath = "Body/Content/RaceRightSide/Head/SequentialSelector/SequentialSelector/GameObject/Frame/Label";
        internal const string InputPath = "Body/Content/CharacterMiddleSide/CharacterName/PointsBox/Bg";
        internal const string ScrollPath = "Body/Content/SkillsLeftSide/MartiaAndSaves/SpellTable/DescriptionView/Scrollbar Vertical";
        internal const string RulePath = "Body/Content/RaceRightSide/Constitution/DescriptionView/Decor (1)";

        internal static NativeThemeResolution Resolve(object owner, INativeThemeSource source)
        {
            var result = new NativeThemeResolution(owner, source);
            var lookup = new NativeUiDonorLookup(source);
            foreach (NativeThemeCapability capability in NativeThemeResolution.Capabilities)
            {
                NativeThemeResource resource = null;
                try
                {
                    resource = ResolveCapability(owner, lookup, capability);
                    // A capability is committed only after all its donors validate.
                    source.Validate(capability, resource.Components);
                    result.Accept(capability, resource);
                }
                catch (Exception exception)
                {
                    result.Reject(capability, exception.Message + (resource == null ? string.Empty : "; " + resource.Identity));
                }
            }
            return result;
        }

        private static NativeThemeResource ResolveCapability(object owner, NativeUiDonorLookup lookup, NativeThemeCapability capability)
        {
            switch (capability)
            {
                case NativeThemeCapability.Paper: return Paths(owner, lookup, NativeThemeComponent.Image, PaperPath);
                case NativeThemeCapability.Buttons: return Paths(owner, lookup, NativeThemeComponent.Button, ActionPath);
                case NativeThemeCapability.Heading: return Paths(owner, lookup, NativeThemeComponent.Text, HeadingPath);
                case NativeThemeCapability.Body: return Paths(owner, lookup, NativeThemeComponent.Text, BodyPath);
                case NativeThemeCapability.Selector: return Paths(owner, lookup, NativeThemeComponent.Text, SelectorPath);
                case NativeThemeCapability.ButtonText:
                    object action = lookup.RequirePath(owner, ActionPath);
                    object label = lookup.RequireLiteralChild(action, ButtonLabelName);
                    return new NativeThemeResource
                    {
                        Nodes = new[] { action, label },
                        Components = new[] { lookup.RequireComponent(label, NativeThemeComponent.Text, "literal child", ButtonLabelName) },
                        Identity = "locator=literal child '" + ButtonLabelName + "'; owner=" + lookup.Location(action)
                    };
                case NativeThemeCapability.Input:
                    NativeThemeResource input = Paths(owner, lookup, NativeThemeComponent.Image, InputPath, InputPath + "/Frame");
                    object field = lookup.RequirePath(owner, InputPath + "/InputField");
                    return new NativeThemeResource
                    {
                        Nodes = new[] { input.Nodes[0], input.Nodes[1], field },
                        Components = new[] { input.Components[0], input.Components[1], lookup.RequireComponent(field, NativeThemeComponent.Input, "path", InputPath + "/InputField") },
                        Identity = input.Identity + "; locator=path '" + InputPath + "/InputField'"
                    };
                case NativeThemeCapability.Scrollbar:
                    NativeThemeResource scroll = Paths(owner, lookup, NativeThemeComponent.Image, ScrollPath, ScrollPath + "/Sliding Area/Handle");
                    return new NativeThemeResource
                    {
                        Nodes = scroll.Nodes,
                        Components = new[] { scroll.Components[0], scroll.Components[1], lookup.RequireComponent(scroll.Nodes[0], NativeThemeComponent.Scrollbar, "path", ScrollPath) },
                        Identity = scroll.Identity
                    };
                case NativeThemeCapability.Ornament: return Paths(owner, lookup, NativeThemeComponent.Image, RulePath);
                default: throw new ArgumentOutOfRangeException(nameof(capability));
            }
        }

        private static NativeThemeResource Paths(object owner, NativeUiDonorLookup lookup, NativeThemeComponent component, params string[] paths)
        {
            var nodes = new object[paths.Length];
            var components = new object[paths.Length];
            for (int index = 0; index < paths.Length; index++)
            {
                nodes[index] = lookup.RequirePath(owner, paths[index]);
                components[index] = lookup.RequireComponent(nodes[index], component, "path", paths[index]);
            }
            return new NativeThemeResource { Nodes = nodes, Components = components, Identity = "locator=path '" + string.Join("', '", paths) + "'; owner=" + lookup.Location(owner) };
        }
    }
}
