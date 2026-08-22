using System;
using System.Reflection;

namespace KingmakerDiceRoller.Integration
{
    internal static class ReflectionAccess
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static MemberInfo RequireInstanceMember(Type type, string name)
        {
            MemberInfo member = FindInstanceMember(type, name);
            if (member == null)
            {
                throw new ContractResolutionException("Required member " + type.FullName + "." + name + " was not found.");
            }

            return member;
        }

        internal static MemberInfo FindInstanceMember(Type type, string name)
        {
            if (type == null) return null;
            PropertyInfo property = type.GetProperty(name, InstanceFlags);
            if (property != null) return property;
            return type.GetField(name, InstanceFlags);
        }

        internal static MemberInfo RequireStaticMember(Type type, string name)
        {
            MemberInfo member = FindStaticMember(type, name);
            if (member == null)
            {
                throw new ContractResolutionException("Required static member " + type.FullName + "." + name + " was not found.");
            }

            return member;
        }

        internal static MemberInfo FindStaticMember(Type type, string name)
        {
            if (type == null) return null;
            PropertyInfo property = type.GetProperty(name, StaticFlags);
            if (property != null) return property;
            return type.GetField(name, StaticFlags);
        }

        internal static Type GetMemberType(MemberInfo member)
        {
            PropertyInfo property = member as PropertyInfo;
            if (property != null) return property.PropertyType;
            FieldInfo field = member as FieldInfo;
            if (field != null) return field.FieldType;
            throw new ArgumentException("Only fields and properties are supported.", nameof(member));
        }

        internal static bool CanWrite(MemberInfo member)
        {
            PropertyInfo property = member as PropertyInfo;
            if (property != null) return property.GetSetMethod(true) != null;
            FieldInfo field = member as FieldInfo;
            if (field != null) return !field.IsInitOnly && !field.IsLiteral;
            return false;
        }

        internal static object Read(MemberInfo member, object target)
        {
            PropertyInfo property = member as PropertyInfo;
            if (property != null) return property.GetValue(target, null);
            FieldInfo field = member as FieldInfo;
            if (field != null) return field.GetValue(target);
            throw new ArgumentException("Only fields and properties are supported.", nameof(member));
        }

        internal static void Write(MemberInfo member, object target, object value)
        {
            PropertyInfo property = member as PropertyInfo;
            if (property != null)
            {
                property.SetValue(target, value, null);
                return;
            }

            FieldInfo field = member as FieldInfo;
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }

            throw new ArgumentException("Only fields and properties are supported.", nameof(member));
        }

        internal static bool TryReadBoolean(object root, string[] paths, out bool value, out string matchedPath)
        {
            value = false;
            matchedPath = null;
            if (root == null || paths == null) return false;
            for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
            {
                object current = root;
                string[] segments = paths[pathIndex].Split('.');
                bool failed = false;
                for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
                {
                    if (current == null)
                    {
                        failed = true;
                        break;
                    }

                    MemberInfo member = FindInstanceMember(current.GetType(), segments[segmentIndex]);
                    if (member == null)
                    {
                        failed = true;
                        break;
                    }

                    current = Read(member, current);
                }

                if (!failed && current is bool)
                {
                    value = (bool)current;
                    matchedPath = paths[pathIndex];
                    return true;
                }
            }

            return false;
        }
    }
}
