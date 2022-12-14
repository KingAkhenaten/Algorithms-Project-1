// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace Project_1;

public static class Program
{
    // Author Christian
    public static void Main()
    {
        var serviceProvider = new ServiceCollection()
            .AddSerilogLogging()
            .BuildServiceProvider();
        ActivatorUtilities
            .CreateInstance<MatrixMult>(serviceProvider)
            .Multiply();
    }

    // Author Christian
    private static IServiceCollection AddSerilogLogging(this IServiceCollection services)
    {
        var providers = new LoggerProviderCollection();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss.ffff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Providers(providers)
            .CreateLogger();

        return services
            .AddSingleton(providers)
            .AddSingleton<ILoggerFactory>(serviceProvider =>
            {
                var providerCollection = serviceProvider.GetService<LoggerProviderCollection>();
                var factory = new SerilogLoggerFactory(null, true, providerCollection);

                foreach (var provider in serviceProvider.GetServices<ILoggerProvider>())
                    factory.AddProvider(provider);

                return factory;
            })
            .AddLogging()
            .AddSingleton(serviceProvider =>
                serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultLogger"));
    }
}

public class MatrixMult
{
    private int _count;
    private readonly ILogger<MatrixMult> _logger;
    private int _time;

    public MatrixMult(ILogger<MatrixMult> logger)
    {
        _logger = logger;
        _count = 0;
    }

    // Author Christian
    public void Multiply()
    {
        // find current path
        string path = System.IO.Directory.GetCurrentDirectory();

        // remove csv file content if present
        try
        {
            File.WriteAllText((path + "\\data.csv"), String.Empty);
        }
        catch (NullReferenceException)
        {
            _logger.LogInformation("No CVS to remove");
        }

        // set up csv file for gathering data
        StreamWriter sw = new StreamWriter(path + "\\data.csv");
        sw.AutoFlush = true;

        int [] testSizes = {2,4,8,16,32,64,128,256};
        int [,] a, b, c;

        sw.WriteLine("n, NaiveT, NaiveC, MMRT, MMRC, StrassenT, StrassenC");
        foreach (int n in testSizes)
        {
            a = genM(n,n);
            b = genM(n,n);
            c = new int[n,n];
            sw.Write("{0}, ", n);

            _count = 0;
            c = Naive(a, b, n);
            sw.Write("{0}, ", _time);
            sw.Write("{0}, ", _count);

            _count = 0;
            c = MMR(a, b, n);
            sw.Write("{0}, ", _time);
            sw.Write("{0}, ", _count);

            _count = 0;
            c = Strassen(a, b);
            sw.Write("{0}, ", _time);
            sw.WriteLine("{0}", _count);
        }

        /*
        var n = 16;   
        int[,] a = genM(n,n);
        int[,] b = genM(n,n);
        int[,] c = new int[n,n];
        
        _count = 0;
        c = Naive(a, b, n);
        _count = 0;
        c = MMR(a, b, n);
        _count = 0;
        c = Strassen(a, b);        
        _logger.LogInformation("All done");
        */
    }
    
    // Author Christian
    private int[,] Naive(int [,] a, int[,] b, int n)
    {
        int[,] c = new int[n,n];
        var watch = new System.Diagnostics.Stopwatch();
        watch.Start();
        _logger.LogInformation("Start Naive Approach");
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                for (int k = 0; k < n; k++)
                {
                    _count += 1;
                    c[i, j] = c[i, j] + a[i, k] * b[k, j];
                }
            }
        }

        _logger.LogInformation("Naive: Ended with {count} operations and {time} ms elapsed time",
            _count, watch.ElapsedMilliseconds);
        _time = (int)watch.ElapsedMilliseconds;
        return c;
        
    }
    
    // Author: Christian + Charles + the ghost in my room
    private int [,] MMR(int[,] A, int[,] B, int n, int depth = 0)
    {
        var watch = new System.Diagnostics.Stopwatch();
        int[,] C = new int[n,n];
        
        watch.Start();
        
        if (depth == 0)
            _logger.LogInformation("Start MMR Approach");
        //_logger.LogInformation("MMR Reeecurshon depth:{depth}", depth);

        if (A.GetLength(0) <= 1)
        { 
            // skadoosh
            C[0, 0] = A[0, 0] * B[0, 0];
            _count += 1;
            return C;
            // Conquer
        }

        int[,] A0, A1, A2, A3, B0, B1, B2, B3;
        // divide
        int [,][,] subA = splitM(A);
        int [,][,] subB = splitM(B);
        // int [,][,] subC = splitM(C);
        // assign sub-matrices
        A0 = subA[0,0];
        A1 = subA[0,1];
        A2 = subA[1,0];
        A3 = subA[1,1];
            
        B0 = subB[0,0];
        B1 = subB[0,1];
        B2 = subB[1,0];
        B3 = subB[1,1];
        
        // C0 = subC[0, 0];
        // C1 = subC[0, 1];
        // C2 = subC[1, 0];
        // C3 = subC[1, 1];
            
        int [,] C00 = addM(MMR(A0, B0, n/2, depth + 1), MMR(A1, B2, n/2, depth + 1));
        int [,] C01 = addM(MMR(A0, B1, n/2, depth + 1), MMR(A1, B3, n/2, depth + 1));
        int [,] C10 = addM(MMR(A2, B0, n/2, depth + 1), MMR(A3, B2, n/2, depth + 1));
        int [,] C11 = addM(MMR(A2, B1, n/2, depth + 1), MMR(A3, B3, n/2, depth + 1));
            
        // pad for merging
        // this resizes all the quadrant matrices to be the same size as C
        // each quadrant is shifted into place and all other spaces are 0s
        // this way they can simply be added to C for merging
        C00 = padM(C00, C.GetLength(0)-C00.GetLength(0), C.GetLength(1)-C00.GetLength(1));
        C01 = padM(C01, C.GetLength(0)-C01.GetLength(0), C.GetLength(1)-C01.GetLength(1), 0, C.GetLength(1)-C01.GetLength(1));
        C10 = padM(C10, C.GetLength(0)-C10.GetLength(0), C.GetLength(1)-C10.GetLength(1), C.GetLength(0)-C10.GetLength(0), 0);
        C11 = padM(C11, C.GetLength(0)-C11.GetLength(0), C.GetLength(1)-C11.GetLength(1), C.GetLength(0)-C11.GetLength(0), C.GetLength(1)-C11.GetLength(1));
            
        // reform quadrants into C / result
        C = addM(addM(addM(addM(C,C00),C01),C10),C11);

        if (depth == 0) _logger.LogInformation("MMR ended with {count} operations and {time} ms elapsed time, Ahahaaa...", _count, watch.ElapsedMilliseconds);
        _time = (int)watch.ElapsedMilliseconds;
        return C;
    }

    
    // Strassen matrix multiplication method
    // matrix a & matrix b are multiplied together using the strassen method
    // depth is an optional parameter for measuring recursion in logs
    // Author: Charles
    private int [,] Strassen(int [,] A, int[,] B, int depth = 0)
    {
        // count and watch variables for diagnostics
        var watch = new System.Diagnostics.Stopwatch();
        int [,] C = new int [A.GetLength(0),A.GetLength(1)]; // return matrix

        // start diagnostics & logging
        watch.Start();
        if (depth == 0)
            _logger.LogInformation("Start Strassen Approach");
        //else
            //_logger.LogInformation("Strassen Recursion, Depth {depth}", depth);

        // check for sufficiently small matrix for naive
        // TODO add rectangular support (pad with 0s above?)
        if (A.GetLength(0) <= 1)
        {
            C[0,0] = A[0,0] * B[0,0];
            _count += 1;
            return C;
        }

        // split original matrices into 8 separate matrices
        // initialize new matrices
        // Visualization:
        // A = a b     B = e f
        //     c d         g h
        int [,] a, b, c, d, e, f, g, h;
        // separate original matrices into quadrants
        int [,][,] quadA = splitM(A);
        int [,][,] quadB = splitM(B);
        
        // assign sub-matrices
        a = quadA[0,0];
        b = quadA[0,1];
        c = quadA[1,0];
        d = quadA[1,1];

        e = quadB[0,0];
        f = quadB[0,1];
        g = quadB[1,0];
        h = quadB[1,1]; 
        
        // Strassen recursion
        int [,] m1 = Strassen(addM(a,d), addM(e,h), depth+1),
                m2 = Strassen(addM(c,d), e, depth+1),
                m3 = Strassen(a, subM(f,h), depth+1),
                m4 = Strassen(d, subM(g,e), depth+1),
                m5 = Strassen(addM(a,b), h, depth+1),
                m6 = Strassen(subM(c,a), addM(e,f), depth+1),
                m7 = Strassen(subM(b,d), addM(g,h), depth+1);

        // solve quadrants
        // note:
        // C00 = (m1+m4)-(m5-m7) = m1+m4-m5+m7 
        // C11 = (m1+m3)-(m2-m6) = m1+m3-m2+m6
        int [,] C00 = subM(addM(m1,m4),subM(m5,m7)),    // C00 = (m1+m4)-(m5-m7)
                C01 = addM(m3,m5),                      // C01 = m3+m5
                C10 = addM(m2,m4),                      // c10 = m2+m4
                C11 = subM(addM(m1,m3),subM(m2,m6));    // C11 = (m1+m3)-(m2-m6)

        // pad for merging
        // this resizes all the quadrant matrices to be the same size as C
        // each quadrant is shifted into place and all other spaces are 0s
        // this way they can simply be added to C for merging
        C00 = padM(C00, C.GetLength(0)-C00.GetLength(0), C.GetLength(1)-C00.GetLength(1));
        C01 = padM(C01, C.GetLength(0)-C01.GetLength(0), C.GetLength(1)-C01.GetLength(1), 0, C.GetLength(1)-C01.GetLength(1));
        C10 = padM(C10, C.GetLength(0)-C10.GetLength(0), C.GetLength(1)-C10.GetLength(1), C.GetLength(0)-C10.GetLength(0), 0);
        C11 = padM(C11, C.GetLength(0)-C11.GetLength(0), C.GetLength(1)-C11.GetLength(1), C.GetLength(0)-C11.GetLength(0), C.GetLength(1)-C11.GetLength(1));

        // reform quadrants into C / result
        C = addM(addM(addM(addM(C,C00),C01),C10),C11);

        // report log
        if (depth == 0) _logger.LogInformation("Strassen: Ended with {count} operations and {time} ms elapsed time",
            _count, watch.ElapsedMilliseconds);
        _time = (int)watch.ElapsedMilliseconds;

        // return
        return C;
    }
    
    // MATRIX OPERATIONS
    //-------------------
    // TODO? consider making this more eligant
    // by adding operators to int matrices
    // (if that's possible)
    // or make this a separate class

    // Method for splitting matrices into quadrants
    // this method takes m matrix and returns a matrix
    // of matrices, where each matrix is a sub-matrix of the
    // corresponding quadrant of matrix m.
    // EXAMPLE:
    //
    // m = 1 matrix:    ret = 4 matrices:
    // 1 2 4 2          1 2 | 4 2
    // 3 4 2 1          3 4 | 2 1
    // 2 6 1 7          ---------
    // 7 9 3 4          2 6 | 1 7
    //                  7 9 | 3 4
    //
    // it would probably be more efficient to simply use an array
    // ,but this seemed better for visualization and frame of reference
    private int [,][,] splitM(int [,] m)
    {
        // variables
        int h = m.GetLength(0);                     // m height or max row
        int w = m.GetLength(1);                     // m width or max column
        int [,][,] ret = new int [2,2][,];          // return matrices

        // check input
        if (h < 2 || w < 2)
            throw new ArgumentException("ERROR: method splitM requires a matrix of size 2x2 or greater");

        // quadrant II / upper left
        ret[0,0] = new int [h/2,w/2];
        for (int i = 0; i < h/2; i++)
            for (int j = 0; j < w/2; j++)
            {
                ret[0,0][i,j] = m[i,j];
            }

        // quadrant I / upper right
        ret[0,1] = new int [h/2,w/2];
        for (int i = 0; i < h/2; i++)
            for (int j = 0; j < w/2; j++)
            {
                ret[0,1][i,j] = m[i,j+w/2];
            }
        
        // quadrant III / bottom left
        ret[1,0] = new int [h/2,w/2];
        for (int i = 0; i < h/2; i++)
            for (int j = 0; j < w/2; j++)
            {
                ret[1,0][i,j] = m[i+h/2,j];
            }

        // quadrant IV / bottom right
        ret[1,1] = new int [h/2,w/2];
        for (int i = 0; i < h/2; i++)
            for (int j = 0; j < w/2; j++)
            {
                ret[1,1][i,j] = m[i+h/2,j+w/2];
            }

        return ret;
    }

    // method for matrix addition
    private int [,] addM(int [,] a, int [,] b)
    {
        // return variable
        int [,] ret = new int [a.GetLength(0), a.GetLength(1)];

        // input check
        if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
            throw new ArgumentException("ERROR: addM requires matrices a & b to be of equal dimension, consider padding the matrix");

        // sum at each index of the matrices and store for return
        for (int i = 0; i < a.GetLength(0); i++)
            for (int j = 0; j < a.GetLength(1); j++)
                ret[i,j] = a[i,j] + b[i,j];

        return ret;
    }

        // method for matrix addition
    private int [,] subM(int [,] a, int [,] b)
    {
        // return variable
        int [,] ret = new int [a.GetLength(0), a.GetLength(1)];

        // input check
        if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1))
            throw new ArgumentException("ERROR: subM requires matrices a & b to be of equal dimension, consider padding the matrix");

        // subtract at each index of the matrices and store for return
        for (int i = 0; i < a.GetLength(0); i++)
            for (int j = 0; j < a.GetLength(1); j++)
                ret[i,j] = a[i,j] - b[i,j];

        return ret;
    }

    // method for padding matrices with 0s
    // pads matrix m with 'rowPad' number of rows & 'colPad' number of columns
    // optional parameters rowOff and colOff can be used to offset values of m
    // into the new padded matrix
    // i.e. rowPad = 1 would add an additional row of 0s on the bottom
    //      adding rowOff = 1 would shift the m values, leaving the 0s on the top
    private int [,] padM(int [,] m, int rowPad, int colPad, int rowOff = 0, int colOff = 0)
    {
        // return variable
        int mRows = m.GetLength(0);
        int mCols = m.GetLength(1);
        int [,] ret = new int [mRows+rowPad, mCols+colPad];

        // input check TODO

        // insert m values and 0 padding into return array
        for (int i = 0; i < ret.GetLength(0); i++)
            for (int j = 0; j < ret.GetLength(1); j++)
            {
                if (i < rowOff 
                    || j < colOff
                    || i >= rowOff + mRows
                    || j >= colOff + mCols)
                    ret[i,j] = 0;
                else
                {
                    try
                    {
                        ret[i,j] = m[i-rowOff,j-colOff];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        _logger.LogInformation("Warning: padM offset out of bounds");
                    }
                }
                    
            }
        
        return ret;
    }

    // method for generating matrices given
    // r & c (number of rows and columns) and
    // optionally the min and max of the generated values
    private int[,] genM(int r, int c, int min = 1, int max = 10)
    {
        // input checking
        if (r < 1 || c < 1)
            return new int [0,0];
        
        // variables
        int [,] ret = new int [r,c];    // return matrix
        Random rand = new Random();     // Random instance

        // fill ret with rand values where 
        // min <= v < max
        for (int i = 0; i < r; i++)
            for (int j = 0; j < c; j++)
                ret[i,j] = rand.Next(min, max);

        return ret;
    }

}
