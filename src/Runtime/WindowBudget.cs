namespace HowToLua;

internal sealed class WindowBudget
{
    private double _started = double.NegativeInfinity;
    private int _used;
    internal bool Take(double now, int limit, double seconds)
    {
        if (now < _started || now - _started >= seconds) { _started = now; _used = 0; }
        if (_used >= limit) return false;
        _used++;
        return true;
    }
}
