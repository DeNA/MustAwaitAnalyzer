namespace MyNamespace;

public class Program04
{
    public static int _i;
    public static int _j;
    public void M1()
    {
        bool c;
        _j = 10;
        _i = -8;
        
        L3:
        _i = _i + 1;
        
        L4:
        _j = _j - 1;
        c = _j != 0;
        if (c) goto L3;

        _j = _i / 2;
        c = _i < 8;
        if (c) goto L11;
        
        _i = 2;
        
        L11:
        goto L4;
    }
}
