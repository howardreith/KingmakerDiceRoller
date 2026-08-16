using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerDiceRoller.DomainTests
{
    internal static class AssertEx
    {
        internal static void Equal<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException((message ?? "Values differ.") + " Expected: " + expected + "; actual: " + actual + ".");
            }
        }

        internal static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null)
        {
            if (!expected.SequenceEqual(actual))
            {
                throw new InvalidOperationException((message ?? "Sequences differ.") + " Expected: [" + string.Join(",", expected) + "]; actual: [" + string.Join(",", actual) + "].");
            }
        }

        internal static void True(bool value, string message = null)
        {
            if (!value) throw new InvalidOperationException(message ?? "Expected true.");
        }

        internal static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException exception)
            {
                return exception;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
