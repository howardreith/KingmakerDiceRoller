using System;
using System.Collections.Generic;

namespace KingmakerDiceRoller.UI
{
    // Shared by the shipped Unity adapter and deterministic hierarchy fixtures.
    // Nodes/components are opaque here; only the adapter touches Unity objects.
    internal interface INativeThemeSource
    {
        bool IsAlive(object value);
        bool SameNode(object first, object second);
        string Name(object node);
        object Parent(object node);
        int ChildCount(object node);
        object Child(object node, int index);
        object[] Components(object node, NativeThemeComponent component);
        void Validate(NativeThemeCapability capability, object[] components);
    }

    internal enum NativeThemeComponent { Image, Button, Text, Input, Scrollbar }

    internal sealed class NativeUiDonorLookup
    {
        internal const int MaximumChildren = 512;
        internal const int MaximumDepth = 32;
        private readonly INativeThemeSource source;

        internal NativeUiDonorLookup(INativeThemeSource source) { this.source = source; }

        internal object RequirePath(object owner, string path)
        {
            if (string.IsNullOrEmpty(path)) throw Failure(owner, "path", path, "empty path");
            string[] parts = path.Split('/');
            if (parts.Length > MaximumDepth) throw Failure(owner, "path", path, "path depth exceeds limit");
            object current = owner;
            foreach (string part in parts)
            {
                if (part.Length == 0) throw Failure(current, "path", path, "empty path segment");
                current = FindDirect(current, part, "path", path);
            }
            return current;
        }

        internal object RequireLiteralChild(object owner, string name)
        {
            if (string.IsNullOrEmpty(name)) throw Failure(owner, "literal child", name, "empty name");
            return FindDirect(owner, name, "literal child", name);
        }

        internal object RequireComponent(object node, NativeThemeComponent component, string kind, string locator)
        {
            if (!source.IsAlive(node)) throw Failure(node, kind, locator, "owner missing/destroyed");
            object[] values = source.Components(node, component);
            if (values == null || values.Length == 0)
                throw Failure(node, kind, locator, "expected component missing: " + component);
            if (values.Length != 1)
                throw Failure(node, kind, locator, "ambiguous expected components: " + component + " (" + values.Length + ")");
            if (!source.IsAlive(values[0]))
                throw Failure(node, kind, locator, "expected component destroyed: " + component);
            return values[0];
        }

        private object FindDirect(object owner, string name, string kind, string locator)
        {
            if (!source.IsAlive(owner)) throw Failure(owner, kind, locator, "owner missing/destroyed");
            int count = source.ChildCount(owner);
            if (count > MaximumChildren) throw Failure(owner, kind, locator, "direct-child count exceeds " + MaximumChildren);
            object match = null;
            int matches = 0;
            for (int index = 0; index < count; index++)
            {
                object child = source.Child(owner, index);
                // Deliberately no activeSelf/activeInHierarchy filter.
                if (source.IsAlive(child) && string.Equals(source.Name(child), name, StringComparison.Ordinal))
                {
                    match = child;
                    matches++;
                }
            }
            if (matches == 0) throw Failure(owner, kind, locator, "child not found: " + Quote(name));
            if (matches != 1) throw Failure(owner, kind, locator, "ambiguous direct children: " + Quote(name) + " (" + matches + ")");
            return match;
        }

        internal bool IsUnderOwner(object node, object owner)
        {
            if (!source.IsAlive(owner)) return false;
            for (int depth = 0; depth < MaximumDepth && source.IsAlive(node); depth++, node = source.Parent(node))
                if (source.SameNode(node, owner)) return true;
            return false;
        }

        internal string Location(object node)
        {
            var names = new List<string>();
            int depth = 0;
            while (source.IsAlive(node) && depth++ < 12)
            {
                names.Add(Quote(source.Name(node)));
                node = source.Parent(node);
            }
            if (source.IsAlive(node)) names.Add("...");
            names.Reverse();
            return names.Count == 0 ? "<missing/destroyed>" : string.Join(" > ", names);
        }

        private InvalidOperationException Failure(object owner, string kind, string locator, string reason)
        {
            var children = new List<string>();
            if (source.IsAlive(owner))
            {
                int count = source.ChildCount(owner);
                for (int index = 0; index < Math.Min(count, 6); index++)
                {
                    object child = source.Child(owner, index);
                    children.Add(source.IsAlive(child) ? Quote(source.Name(child)) : "<destroyed>");
                }
                if (count > 6) children.Add("... (" + count + " children)");
            }
            return new InvalidOperationException(reason + "; locator=" + kind + " " + Quote(locator) +
                "; owner=" + Location(owner) + "; direct children=[" + string.Join(", ", children) + "]");
        }

        private static string Quote(string value)
        {
            value = (value ?? "<null>").Replace("\r", " ").Replace("\n", " ").Replace("'", "''");
            return "'" + (value.Length <= 160 ? value : value.Substring(0, 157) + "...") + "'";
        }
    }
}
