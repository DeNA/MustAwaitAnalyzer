namespace MyNamespace;

public class Program01
{
    public void M1(bool b1, bool b2, bool b3) // BB0
    {
        int a, b, c, d, e, f, g, some;
        L:
        a = 1; // BB1
        if (b1)
        {
            b = 2; // BB2
            some = 0; // BB2
        }
        else
        {
            c = 3; // BB3
            if (b2)
            {
                d = 4; // BB4
            }
            else
            {
                e = 5; // BB5
            }

            f = 6; // BB6
        }

        g = 7; // BB7
        if (b3)
        {
            goto L;
        }
    } // BB8
}
