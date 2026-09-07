using System;
using System.Collections;

namespace HowToLua;

internal static class ObservedRoutine
{
    internal static IEnumerator Wrap(IEnumerator original, Action completed)
    {
        try
        {
            while (original.MoveNext()) yield return original.Current;
            completed();
        }
        finally { (original as IDisposable)?.Dispose(); }
    }
}
