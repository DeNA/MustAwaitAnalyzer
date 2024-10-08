namespace MyNamespace;

public class Program02
{
    public void M1()
    {
        int i, j;
        bool c;
        j = 10;
        i = -8;
        
        L3:
        i = i + 1;
        
        L4:
        j = j - 1;
        c = j != 0;
        if (c) goto L3;

        j = i / 2;
        c = i < 8;
        if (c) goto L11;

        i = 2;
        
        L11:
        goto L4;
    }
}
