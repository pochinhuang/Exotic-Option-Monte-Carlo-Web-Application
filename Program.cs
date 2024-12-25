using System;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;


/*
This program is a simple interface about Monte Carlo simulation.
User will enter the parameters including Stock Price, Strike Price, Risk Free Rate,
Volatility, Time to Maturity (Year), Number of Steps, and Number of Simulations.
Then the interface will show option price, standard error, and greeks for both call and put.

FM5252 HW6 updates:
1. Low Discrepancy Sequences
2. Antithetic variance reduction

The user will be asked to enter 2 int as base for Low Discrepancy Sequences.
Also an additional Y/N choice to use Antithetic variance reduction or not.

FM5353 updates:

    HW01:
        1. New option of "Use Delta-Based Control Variate?"
        2. New configuration Delta-Baesd Control Variate
        3. New configuration Antithetic variance reduction + Delta-Baesd Control Variate
        4. Updated Greeks class for calculating 4 configurations
        5. Separate Price and Standard Error functions to a new class

    HW02:
        1. New option of "Enable Multithreading Parallel Execution?"
        2. Added StopWatch show Computation time
    
    HW03:
        1. New payoff function for Exotic options
        2. Added "Method" and "Type" to show user's choices
    
    HW04:
        http://localhost:{port}/api/OptionPricing

        json:
        {
        "s": 0,
        "k": 0,
        "r": 0,
        "sigma": 0,
        "t": 0,
        "steps": 0,
        "n": 0,
        "antithetic": true,
        "controlVariate": true,
        "parallelExecution": true,
        "optionClass": "string",
        "barrierClass": "string",
        "p": 0,
        "h": 0,
        "lookBackClass": "string"
        }

        S: Stock Price
        K: Strike Price
        r: Risk-free Rate
        sigma: Volatility
        t: Time to Maturity
        steps: Number of Steps
        n: Number of Simulations
        antithetic: Antithetic Variance Reduction Method
        controlVariate: Control Variate Method
        parallelExecution: Multi Threading Parallel Execution
        p: Payout (For Digital Option )
        h: Barrier Level (For Barrier Option )

        optionClass:
            E: European Option
            A: Asian Option
            D: Digital Option
            B: Barrier Option
            L: Lookback Option
            R: Range Option

        (nullable)
        barrierClass:
            UI: Up-and-In
            DI: Down-and-In
            UO: Up-and-Out
            DO: Down-and-Out

        (nullable)
        lookBackClass:
            FT: Floating Strike Lookback Option
            FX: Fixed Strike Lookback Option
    
    HW05:
        1. Create web application
        2. User input the parameters and select the methods
        3. Results will be displayed on the right side table


Refferences:

    FM 5252:
        https://stackoverflow.com/questions/5336457/how-to-calculate-a-standard-deviation-array
        https://stackoverflow.com/questions/13813166/read-user-input-of-double-type
        https://stackoverflow.com/questions/42926301/how-to-return-multiple-values-in-c-sharp-7
        https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings

    FM5353
        HW01:
            https://github.com/BenJ-cell/Monte-Carlo-Variance-Reduction-Methods-Antithetic-Delta-and-Gamma-based-Control-Variates/blob/main/Monte_Carlo_Variance_Reduction_Methods_%E2%80%93_Antithetic%2C_Delta_and_Gamma_based_Control_Variates.ipynb
            https://stackoverflow.com/questions/43653560/how-do-i-fit-cumulative-distribution-function-of-normal-distribution-to-data-poi
        
        HW02:
            https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel.for?view=net-8.0
            https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.paralleloptions?view=net-8.0
            https://dotnettutorials.net/lesson/maximum-degree-of-parallelism-in-csharp/
            https://stackoverflow.com/questions/55686928/using-stopwatch-in-c-sharp

*/
namespace FM5353_HW05
{
    class Program
    {
        public static void Main(string[] args)
        {
            // build and run the application host
            CreateHostBuilder(args).Build().Run();
        }

        // set up the application host builder
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices((context, services) =>
                    {
                        services.AddControllers();
                        services.AddScoped<MonteCarlo>();

                        // Add CORS policy
                        services.AddCors(options =>
                        {
                            options.AddPolicy("AllowAll", builder =>
                                builder.AllowAnyOrigin()
                                       .AllowAnyMethod()
                                       .AllowAnyHeader());
                        });

                        // Add Swagger generation
                        services.AddSwaggerGen(c =>
                        {
                            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                            {
                                Title = "Monte Carlo Option Pricing API",
                                Version = "v1",
                                Description = "API for simulating Monte Carlo option pricing and calculating Greeks."
                            });
                        });
                    });

                    webBuilder.Configure(app =>
                    {
                        var env = app.ApplicationServices.GetService<IHostEnvironment>();

                        if (env != null && env.IsDevelopment())
                        {
                            app.UseDeveloperExceptionPage();
                            app.UseSwagger();
                            app.UseSwaggerUI(c =>
                            {
                                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Monte Carlo Option Pricing API v1");
                                c.RoutePrefix = string.Empty; // Swagger UI available at root URL
                            });
                        }

                        app.UseCors("AllowAll");
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                });
    }

// namespace FM5353_HW05
// {
//     class Program
//     {
//         public static void Main(string[] args)
//         {
//             // build and run the application host
//             CreateHostBuilder(args).Build().Run();
//         }

//         public static IHostBuilder CreateHostBuilder(string[] args) =>
//             Host.CreateDefaultBuilder(args)
//                 .ConfigureWebHostDefaults(webBuilder =>
//                 {
//                     webBuilder.ConfigureServices(services =>
//                     {
//                         services.AddControllers();

//                         services.AddCors(options =>
//                         {
//                             options.AddPolicy("AllowAll", builder =>
//                                 builder.AllowAnyOrigin()
//                                        .AllowAnyMethod()
//                                        .AllowAnyHeader()); 
//                         });
//                     })
//                     .Configure(app =>
//                     {
//                         app.UseCors("AllowAll");

//                         app.UseRouting();
//                         app.UseEndpoints(endpoints =>
//                         {
//                             endpoints.MapControllers();
//                         });
//                     });
//                 });
//     }

    [Route("api/[controller]")]
    [ApiController]
    public class MonteCarloController : ControllerBase
    {
        [HttpPost("simulate")]
        public ActionResult<MonteCarloResult> Simulate([FromBody] MonteCarloInput input)
        {
            // Random generateor
            NormalGenerator generator = new NormalGenerator();
            double[,] normal = generator.Generate(input.NumberOfSimulations, input.NumberOfSteps);

            // run montecarlo
            MonteCarlo monteCarlo = new MonteCarlo();
            (double[] SimulateCall, double[] SimulatePut) = monteCarlo.Simulation(
                input.StockPrice, input.StrikePrice, input.RiskFreeRate, input.Volatility, 
                input.TimeToMaturity, input.NumberOfSteps, input.NumberOfSimulations, 
                normal, input.UseAntithetic, input.ControlVariate, input.parallelExecution, input.OptionClass, input.BarrierClass, 
                input.P, input.H, input.LookBackClass);

            // calculate price and standard error
            Results result = new Results();
            (double CallPrice, double PutPrice) = result.Price(SimulateCall, SimulatePut, input.RiskFreeRate, input.TimeToMaturity);
            (double CallSE, double PutSE) = result.StandardError(SimulateCall, SimulatePut);

            // calculate greeks
            Greeks greeks = new Greeks(
                input.StockPrice, input.StrikePrice, input.RiskFreeRate, input.Volatility, 
                input.TimeToMaturity, input.NumberOfSteps, input.NumberOfSimulations, 
                normal, input.UseAntithetic, input.ControlVariate, input.parallelExecution, input.OptionClass, input.BarrierClass, 
                input.P, input.H, input.LookBackClass);

            (double CallDelta, double PutDelta) = greeks.Delta();
            (double CallGamma, double PutGamma) = greeks.Gamma();
            (double CallVega, double PutVega) = greeks.Vega();
            (double CallTheta, double PutTheta) = greeks.Theta();
            (double CallRho, double PutRho) = greeks.Rho();

            return Ok(new MonteCarloResult {CallPrice = CallPrice, 
                                            PutPrice = PutPrice,
                                            CallSE = CallSE,
                                            PutSE = PutSE,
                                            CallDelta = CallDelta,
                                            PutDelta = PutDelta,
                                            CallGamma = CallGamma,
                                            PutGamma = PutGamma,
                                            CallVega = CallVega,
                                            PutVega = PutVega,
                                            CallTheta = CallTheta,
                                            PutTheta = PutTheta,
                                            CallRho = CallRho,
                                            PutRho = PutRho,});
        }
    }

    public class MonteCarloInput
    {
        public double StockPrice { get; set; }
        public double StrikePrice { get; set; } = 0;
        public double RiskFreeRate { get; set; }
        public double Volatility { get; set; }
        public double TimeToMaturity { get; set; }
        public int NumberOfSteps { get; set; }
        public int NumberOfSimulations { get; set; }
        public bool UseAntithetic { get; set; }
        public bool ControlVariate { get; set; }
        public bool parallelExecution { get; set; }
        public string OptionClass { get; set; }
        public string? BarrierClass { get; set; }
        public double P { get; set; } = 0;
        public double H { get; set; } = 0;
        public string? LookBackClass { get; set; }

    }

    public class MonteCarloResult
    {
        public double CallPrice { get; set; }
        public double PutPrice { get; set; }
        public double CallSE { get; set; }
        public double PutSE { get; set; }
        public double CallDelta { get; set; }
        public double PutDelta { get; set; }
        public double CallGamma { get; set; }
        public double PutGamma { get; set; }
        public double CallVega { get; set; }
        public double PutVega { get; set; }
        public double CallTheta { get; set; }
        public double PutTheta { get; set; }
        public double CallRho { get; set; }
        public double PutRho { get; set; }
    }


    // Payoff Function Class
    class Payoff
    {
        public double func(double ST, double K, string OptionType, string OptionClass, 
        string? BarrierClass = null, double[] STs = null, double? P = 0, double? H = 0, 
        string? LookBackClass = null)
        {
            // european option
            if (OptionClass == "E")
            {
                if (OptionType == "call")
                {
                    return Math.Max(ST - K, 0);
                }
                else
                {
                    return Math.Max(K - ST, 0);
                }                
            }
            // asian option
            else if (OptionClass == "A")
            {
                double STMean = STs.Average();

                if (OptionType == "call")
                {
                    return Math.Max(STMean - K, 0);
                }
                else
                {
                    return Math.Max(K - STMean, 0);
                }                  
            }
            // digital option
            else if (OptionClass == "D")
            {
                if (OptionType == "call")
                {
                    if (ST > K)
                    {
                        return P ?? 0;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    if (ST < K)
                    {
                        return P ?? 0;
                    }
                    else
                    {
                        return 0; 
                    }                    
                }
            }
            // barrier option
            else if (OptionClass == "B")
            {
                // Up-and-In
                if (BarrierClass == "UI")
                {
                    bool TouchedBarrier = STs.Any(s => s >= H);

                    if (TouchedBarrier)
                    {
                        if (OptionType == "call")
                        {
                            return Math.Max(ST - K, 0);
                        }
                        else
                        {
                            return Math.Max(K - ST, 0);
                        }
                    }
                    
                    else
                    {
                        return 0;
                    }
                }
                // Down-and-In
                else if (BarrierClass == "DI")
                {
                    bool TouchedBarrier = STs.Any(s => s <= H);

                    if (TouchedBarrier)
                    {
                        if (OptionType == "call")
                        {
                            return Math.Max(ST - K, 0);
                        }
                        else
                        {
                            return Math.Max(K - ST, 0);
                        }
                    }
                    
                    else
                    {
                        return 0;
                    }
                }

                // Up-and-Out
                else if (BarrierClass == "UO")
                {
                    bool TouchedBarrier = STs.Any(s => s >= H);

                    if (TouchedBarrier)
                    {
                        return 0;
                    }
                    
                    else
                    {
                        if (OptionType == "call")
                        {
                            return Math.Max(ST - K, 0);
                        }
                        else
                        {
                            return Math.Max(K - ST, 0);
                        }
                    }
                }

                // Down-and-Out
                else if (BarrierClass == "DO")
                {
                    bool TouchedBarrier = STs.Any(s => s <= H);

                    if (TouchedBarrier)
                    {
                        return 0;
                    }
                    
                    else
                    {
                        if (OptionType == "call")
                        {
                            return Math.Max(ST - K, 0);
                        }
                        else
                        {
                            return Math.Max(K - ST, 0);
                        }
                    }
                }
            }
            // lookback option
            else if (OptionClass == "L")
            {   
                // floating skrike
                if (LookBackClass == "FT")
                {
                    if (OptionType == "call")
                    {
                        return Math.Max(ST - STs.Min(), 0);
                    }
                    else
                    {
                        return Math.Max(STs.Max() - ST, 0);
                    }
                }
                // fixed strike
                else if (LookBackClass == "FX")
                {
                    if (OptionType == "call")
                    {
                        return Math.Max(STs.Max() - K, 0);
                    }
                    else
                    {
                        return Math.Max(K - STs.Min(), 0);
                    }                    
                }
            }
            // range option
            else if (OptionClass == "R")
            {
                return STs.Max() - STs.Min();
            }
            
            return 0;
        }
    }

    // normally distributed random numbers using the Box-Muller
        class NormalGenerator
    {
        private Random random = new Random();
        // Generate a matrix of standard normal random variables
        public double[,] Generate(int N, int steps)
        {
            double[,] normalMatrix = new double[N, steps];

            // number of simulation
            for (int i = 0; i < N; i++)
            {
                // number of steps
                for (int j = 0; j < steps; j++)
                {
                    // Use Box-Muller to generate normal random numbers
                    double u1 = random.NextDouble();
                    double u2 = random.NextDouble();

                    double z1 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                    // Box-Muller gives two independent standard normal variables
                    normalMatrix[i, j] = z1;
                }
            }

            return normalMatrix;
        }
    }

    // Monte Carlo Class
    class MonteCarlo
    {
        public (double[], double[]) 
        Simulation(double S, double K, double r, double sigma, double T, int steps, int N, double[,] normal, bool Antithetic, bool ControlVariate, 
        bool parallelExecution, string OptionClass, string? BarrierClass = null, double? P = 0, double? H = 0, string? LookBackClass = null)
        
        {
            if (!Antithetic && !ControlVariate)
            {
                Payoff payoff = new Payoff();
                double dt = (double)T / steps;

                double[] CallPayoffs = new double[N];
                double[] PutPayoffs = new double[N];

                // Monte Carlo simulation
                if (parallelExecution)
                {
                    int coreCount = Environment.ProcessorCount;

                    Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = coreCount }, i =>
                    {
                        double ST = S;
                        double[] STs = new double[steps];
                        for (int j = 0; j < steps; j++)
                        {
                            ST *= Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt + sigma * Math.Sqrt(dt) * normal[i, j]);
                            STs[j] = ST;
                        }
                        CallPayoffs[i] = payoff.func(ST, K, "call", OptionClass, BarrierClass, STs, P, H, LookBackClass);
                        PutPayoffs[i] = payoff.func(ST, K, "put", OptionClass, BarrierClass, STs, P, H, LookBackClass);
                    });
                }
                else
                {
                    for (int i = 0; i < N; i++)
                    {
                        double ST = S;
                        double[] STs = new double[steps];
                        for (int j = 0; j < steps; j++)
                        {
                            ST *= Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt + sigma * Math.Sqrt(dt) * normal[i, j]);
                            STs[j] = ST;
                        }
                        CallPayoffs[i] = payoff.func(ST, K, "call", OptionClass, BarrierClass, STs, P, H, LookBackClass);
                        PutPayoffs[i] = payoff.func(ST, K, "put", OptionClass, BarrierClass, STs, P, H, LookBackClass);
                    }
                }
                return (CallPayoffs, PutPayoffs);
            }

            // Antithetic
            else if (Antithetic && !ControlVariate)
            {
                Payoff payoff = new Payoff();
                double dt = (double)T / steps;

                double[] CallPayoffs = new double[N];
                double[] PutPayoffs = new double[N];

                if (parallelExecution)
                {
                    int coreCount = Environment.ProcessorCount;

                    Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = coreCount }, i =>
                    {
                        double ST_OG = S;
                        double ST_ANTI = S;
                        double[] STs_OG = new double[steps];
                        double[] STs_ANTI = new double[steps];

                        // Generate original and opposite paths
                        for (int j = 0; j < steps; j++)
                        {
                            ST_OG *= Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt + sigma * Math.Sqrt(dt) * normal[i, j]);
                            ST_ANTI *= Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt - sigma * Math.Sqrt(dt) * normal[i, j]);
                            STs_OG[j] = ST_OG;
                            STs_ANTI[j] = ST_ANTI;
                        }

                        // Average
                        CallPayoffs[i] = 0.5 * (payoff.func(ST_OG, K, "call", OptionClass, BarrierClass, STs_OG, P, H, LookBackClass) + 
                        payoff.func(ST_ANTI, K, "call", OptionClass, BarrierClass, STs_ANTI, P, H, LookBackClass));

                        PutPayoffs[i] = 0.5 * (payoff.func(ST_OG, K, "put", OptionClass, BarrierClass, STs_OG, P, H, LookBackClass) + 
                        payoff.func(ST_ANTI, K, "put", OptionClass, BarrierClass, STs_ANTI, P, H, LookBackClass));
                    });
                }

                else
                {
                    for (int i = 0; i < N; i++)
                    {
                        double ST_OG = S;
                        double ST_ANTI = S;
                        double[] STs_OG = new double[steps];
                        double[] STs_ANTI = new double[steps];

                        // Generate original and opposite paths
                        for (int j = 0; j < steps; j++)
                        {
                            ST_OG *= Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt + sigma * Math.Sqrt(dt) * normal[i, j]);
                            ST_ANTI *= Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt - sigma * Math.Sqrt(dt) * normal[i, j]);
                            STs_OG[j] = ST_OG;
                            STs_ANTI[j] = ST_ANTI;                        
                        }

                        // Average
                        CallPayoffs[i] = 0.5 * (payoff.func(ST_OG, K, "call", OptionClass, BarrierClass, STs_OG, P, H, LookBackClass) + 
                        payoff.func(ST_ANTI, K, "call", OptionClass, BarrierClass, STs_ANTI, P, H, LookBackClass));

                        PutPayoffs[i] = 0.5 * (payoff.func(ST_OG, K, "put", OptionClass, BarrierClass, STs_OG, P, H, LookBackClass) + 
                        payoff.func(ST_ANTI, K, "put", OptionClass, BarrierClass, STs_ANTI, P, H, LookBackClass));
                    }
                }
                return (CallPayoffs, PutPayoffs);                
            }

            //https://github.com/BenJ-cell/Monte-Carlo-Variance-Reduction-Methods-Antithetic-Delta-and-Gamma-based-Control-Variates/blob/main/Monte_Carlo_Variance_Reduction_Methods_%E2%80%93_Antithetic%2C_Delta_and_Gamma_based_Control_Variates.ipynb
            // Delta based control variate
            else if (!Antithetic && ControlVariate)
            {
                Payoff payoff = new Payoff();
                Delta Delta = new Delta();

                double dt = T / steps;

                double[] CallPayoffs = new double[N];
                double[] PutPayoffs = new double[N];

                if (parallelExecution)
                {
                    int coreCount = Environment.ProcessorCount;

                    Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = coreCount }, i =>
                    {
                        double ST = S;
                        double CvCall = 0;
                        double CvPut = 0;
                        double[] STs = new double[steps];

                        // Generate original paths and delta control variate for each step
                        for (int j = 0; j < steps; j++)
                        {
                            double DeltaCall = Delta.Based(ST, K, T - j * dt, r, sigma, "call");
                            double DeltaPut = Delta.Based(ST, K, T - j * dt, r, sigma, "put");
                            double STn = ST * Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt + sigma * Math.Sqrt(dt) * normal[i, j]);
                            CvCall += DeltaCall * (STn - ST * Math.Exp(r * dt));
                            CvPut += DeltaPut * (STn - ST * Math.Exp(r * dt));

                            ST = STn;
                            STs[j] = ST;
                        }
                        // Option payoff with control variate adjustment
                        CallPayoffs[i] = payoff.func(ST, K, "call", OptionClass, BarrierClass, STs, P, H, LookBackClass) - CvCall;
                        PutPayoffs[i] = payoff.func(ST, K, "put", OptionClass, BarrierClass, STs, P, H, LookBackClass) - CvPut;                 
                    });
                }
                else
                {
                    for (int i = 0; i < N; i++)
                    {
                        double ST = S;
                        double CvCall = 0;
                        double CvPut = 0;
                        double[] STs = new double[steps];

                        // Generate original paths and delta control variate for each step
                        for (int j = 0; j < steps; j++)
                        {
                            double DeltaCall = Delta.Based(ST, K, T - j * dt, r, sigma, "call");
                            double DeltaPut = Delta.Based(ST, K, T - j * dt, r, sigma, "put");
                            double STn = ST * Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt + sigma * Math.Sqrt(dt) * normal[i, j]);
                            CvCall += DeltaCall * (STn - ST * Math.Exp(r * dt));
                            CvPut += DeltaPut * (STn - ST * Math.Exp(r * dt));

                            ST = STn;
                            STs[j] = ST;
                        }
                        // Option payoff with control variate adjustment
                        CallPayoffs[i] = payoff.func(ST, K, "call", OptionClass, BarrierClass, STs, P, H, LookBackClass) - CvCall;
                        PutPayoffs[i] = payoff.func(ST, K, "put", OptionClass, BarrierClass, STs, P, H, LookBackClass) - CvPut; 
                    }
                }
                return (CallPayoffs, PutPayoffs);                
            }
            
            // Monte Carlo with both Antithetic and Control variate
            else
            {
                Payoff payoff = new Payoff();
                Delta Delta = new Delta(); 

                double[] CallPayoffs = new double[N];
                double[] PutPayoffs = new double[N];

                double dt = T / steps;

                if (parallelExecution)
                {
                    int coreCount = Environment.ProcessorCount;

                    Parallel.For(0, N, new ParallelOptions { MaxDegreeOfParallelism = coreCount }, i =>
                    {
                        double ST_OG = S;
                        double ST_ANTI = S;

                        double CvCall_OG = 0;
                        double CvCall_ANTI = 0;

                        double CvPut_OG = 0;
                        double CvPut_ANTI = 0;
                                        
                        double[] STs_OG = new double[steps];
                        double[] STs_ANTI = new double[steps];

                        // Generate original and opposite paths and delta control variate for each step
                        for (int j = 0; j < steps; j++)
                        {
                            double STn_OG = ST_OG * Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt + sigma * Math.Sqrt(dt) * normal[i, j]);
                            double STn_ANTI =ST_ANTI * Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt - sigma * Math.Sqrt(dt) * normal[i, j]);

                            double DeltaCall_OG = Delta.Based(ST_OG, K, T - j * dt, r, sigma, "call");
                            double DeltaCall_ANTI = Delta.Based(ST_ANTI, K, T - j * dt, r, sigma, "call");

                            double DeltaPut_OG = Delta.Based(ST_OG, K, T - j * dt, r, sigma, "put");
                            double DeltaPut_ANTI = Delta.Based(ST_ANTI, K, T - j * dt, r, sigma, "put");

                            CvCall_OG += DeltaCall_OG * (STn_OG - ST_OG * Math.Exp(r * dt));
                            CvCall_ANTI += DeltaCall_ANTI * (STn_ANTI - ST_ANTI * Math.Exp(r * dt));

                            CvPut_OG += DeltaPut_OG * (STn_OG - ST_OG * Math.Exp(r * dt));
                            CvPut_ANTI+= DeltaPut_ANTI * (STn_ANTI - ST_ANTI * Math.Exp(r*dt));

                            ST_OG = STn_OG;
                            ST_ANTI = STn_ANTI;

                            STs_OG[j] = ST_OG;
                            STs_ANTI[j] = ST_ANTI;
                        }

                        double CallCT_OG = payoff.func(ST_OG, K, "call", OptionClass, BarrierClass, STs_OG, P, H, LookBackClass);
                        double CallCT_ANTI = payoff.func(ST_ANTI, K, "call", OptionClass, BarrierClass, STs_ANTI, P, H, LookBackClass);

                        double PutCT_OG = payoff.func(ST_OG, K, "put", OptionClass, BarrierClass, STs_OG, P, H, LookBackClass);
                        double PutCT_ANTI = payoff.func(ST_ANTI, K, "put", OptionClass, BarrierClass, STs_ANTI, P, H, LookBackClass);

                        // Option payoff with control variate adjustment
                        CallPayoffs[i] = 0.5 * (CallCT_OG - CvCall_OG + CallCT_ANTI - CvCall_ANTI);
                        PutPayoffs[i] = 0.5 * (PutCT_OG - CvPut_OG + PutCT_ANTI - CvPut_ANTI);
                    });            
                }

                else
                {
                    for (int i = 0; i < N; i++)
                    {
                        double ST_OG = S;
                        double ST_ANTI = S;

                        double CvCall_OG = 0;
                        double CvCall_ANTI = 0;

                        double CvPut_OG = 0;
                        double CvPut_ANTI = 0;
                                        
                        double[] STs_OG = new double[steps];
                        double[] STs_ANTI = new double[steps];

                        // Generate original and opposite paths and delta control variate for each step
                        for (int j = 0; j < steps; j++)
                        {
                            double STn_OG = ST_OG * Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt + sigma * Math.Sqrt(dt) * normal[i, j]);
                            double STn_ANTI =ST_ANTI * Math.Exp((r - 0.5 * Math.Pow(sigma, 2)) * dt - sigma * Math.Sqrt(dt) * normal[i, j]);

                            double DeltaCall_OG = Delta.Based(ST_OG, K, T - j * dt, r, sigma, "call");
                            double DeltaCall_ANTI = Delta.Based(ST_ANTI, K, T - j * dt, r, sigma, "call");

                            double DeltaPut_OG = Delta.Based(ST_OG, K, T - j * dt, r, sigma, "put");
                            double DeltaPut_ANTI = Delta.Based(ST_ANTI, K, T - j * dt, r, sigma, "put");

                            CvCall_OG += DeltaCall_OG * (STn_OG - ST_OG * Math.Exp(r * dt));
                            CvCall_ANTI += DeltaCall_ANTI * (STn_ANTI - ST_ANTI * Math.Exp(r * dt));

                            CvPut_OG += DeltaPut_OG * (STn_OG - ST_OG * Math.Exp(r * dt));
                            CvPut_ANTI+= DeltaPut_ANTI * (STn_ANTI - ST_ANTI * Math.Exp(r*dt));

                            ST_OG = STn_OG;
                            ST_ANTI = STn_ANTI;

                            STs_OG[j] = ST_OG;
                            STs_ANTI[j] = ST_ANTI;  
                        }

                        double CallCT_OG = payoff.func(ST_OG, K, "call", OptionClass, BarrierClass, STs_OG, P, H, LookBackClass);
                        double CallCT_ANTI = payoff.func(ST_ANTI, K, "call", OptionClass, BarrierClass, STs_ANTI, P, H, LookBackClass);

                        double PutCT_OG = payoff.func(ST_OG, K, "put", OptionClass, BarrierClass, STs_OG, P, H, LookBackClass);
                        double PutCT_ANTI = payoff.func(ST_ANTI, K, "put", OptionClass, BarrierClass, STs_ANTI, P, H, LookBackClass);

                        // Option payoff with control variate adjustment
                        CallPayoffs[i] = 0.5 * (CallCT_OG - CvCall_OG + CallCT_ANTI - CvCall_ANTI);
                        PutPayoffs[i] = 0.5 * (PutCT_OG - CvPut_OG + PutCT_ANTI - CvPut_ANTI);
                    }
                }
            return (CallPayoffs, PutPayoffs);               
            }
        }
    }

    class Delta
    {
        // Black-Scholes Delta
        public double Based(double S, double K, double T, double r, double sigma, string optionType)
        {
            double d1 = (Math.Log(S / K) + (r + 0.5 * sigma * sigma) * T) / (sigma * Math.Sqrt(T));
            double delta = Phi(d1);

            if (optionType.ToLower() == "call")
            {
                // Call option delta: Φ(d1)
                return delta; 
            }

            else if (optionType.ToLower() == "put")
            {
                // Put option delta: Φ(d1) - 1
                return delta - 1;
            }

            else
            {
                throw new ArgumentException("Invalid option type. Use 'call' or 'put'.");
            }
        }

        // Cumulative distribution function (CDF) for standard normal distribution
        //  https://stackoverflow.com/questions/43653560/how-do-i-fit-cumulative-distribution-function-of-normal-distribution-to-data-poi
        public double Phi(double x)
        {
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x) / Math.Sqrt(2.0);

            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return 0.5 * (1.0 + sign * y);
        }
    }

    // Pass the payoffs here to calculate Prices and Standard Errors
    class Results
    {
        public (double, double) 
        Price(double[] CallPayoffs, double[] PutPayoffs, double r, double T)
        {
            double CallPrice = CallPayoffs.Average() * Math.Exp(-r * T);
            double PutPrice = PutPayoffs.Average() * Math.Exp(-r * T);

            return (CallPrice, PutPrice);
        }

        public (double, double) 
        StandardError(double[] CallPayoffs, double[] PutPayoffs)
        {
            double CallMean = CallPayoffs.Average();
            double CallSumSquares = CallPayoffs.Select(val => (val - CallMean) * (val - CallMean)).Sum();
            double CallStandardDev = Math.Sqrt(CallSumSquares / (CallPayoffs.Length - 1));
            double CallStandardError = CallStandardDev / Math.Sqrt(CallPayoffs.Length);

            double PutMean = PutPayoffs.Average();
            double PutSumSquares = PutPayoffs.Select(val => (val - PutMean) * (val - PutMean)).Sum();
            double PutStandardDev = Math.Sqrt(PutSumSquares / (PutPayoffs.Length - 1));
            double PutStandardError = PutStandardDev / Math.Sqrt(PutPayoffs.Length);

            return (CallStandardError, PutStandardError);
        }
    }
    class Greeks
    {
        private double S;
        private double K;
        private double r;
        private double sigma;
        private double T;
        private int steps;
        private int N;
        private double[,] normal;
        Results results = new Results();
        MonteCarlo montecarlo = new MonteCarlo();
        private bool Antithetic;
        private bool ControlVariate;
        private bool ParallelExecution;
        private string OptionClass;
        private string BarrierClass;
        private double? P;
        private double? H;
        private string LookBackClass;

        public Greeks(double S, double K, double r, double sigma, double T, int steps, int N,  
        double[,] normal, bool Antithetic, bool ControlVariate, bool ParallelExecution,
        string OptionClass, string? BarrierClass = null, double? P = null, double? H = null, string? LookBackClass = null)
        {
            this.S = S;
            this.K = K;
            this.r = r;
            this.sigma = sigma;
            this.T = T;
            this.steps = steps;
            this.N = N;
            this.normal = normal;
            this.Antithetic = Antithetic;
            this.ControlVariate = ControlVariate;
            this.ParallelExecution = ParallelExecution;
            this.OptionClass = OptionClass;
            this.BarrierClass = BarrierClass;
            this.P = P;
            this.H = H;
            this.LookBackClass = LookBackClass;            
        }

        public (double, double) Delta()
        {
            // ΔS
            double DeltaS = S * 0.01;

            // S + ΔS and S - ΔS for call and put

            (double[] CallPlusDelta, double[] PutPlusDelta) = montecarlo.Simulation(S * 1.01, K, r, sigma, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);
            (double[] CallMinusDelta, double[] PutMinusDelta) = montecarlo.Simulation(S * 0.99, K, r, sigma, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);

            (double CallPlusDelta_Price, double PutPlusDelta_Price) = results.Price(CallPlusDelta, PutPlusDelta, r, T);
            (double CallMinusDelta_Price, double PutMinusDelta_Price) = results.Price(CallMinusDelta, PutMinusDelta, r, T);

            // delta call and put
            double CallDelta = (CallPlusDelta_Price - CallMinusDelta_Price) / (2 * DeltaS);
            double PutDelta = (PutPlusDelta_Price - PutMinusDelta_Price) / (2 * DeltaS);

            return(CallDelta, PutDelta);
        }

        public (double, double) Gamma()
        {
            // ΔS
            double DeltaS = S * 0.01;

            // S + ΔS, S, and S - ΔS for call and put
            (double[] Call, double[] Put) = montecarlo.Simulation(S, K, r, sigma, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);
            (double[] CallPlusDelta, double[] PutPlusDelta) = montecarlo.Simulation(S * 1.01, K, r, sigma, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);
            (double[] CallMinusDelta, double[] PutMinusDelta) = montecarlo.Simulation(S * 0.99, K, r, sigma, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);

            (double Call_Price, double Put_Price) = results.Price(Call, Put, r, T);
            (double CallPlusDelta_Price, double PutPlusDelta_Price) = results.Price(CallPlusDelta, PutPlusDelta, r, T);
            (double CallMinusDelta_Price, double PutMinusDelta_Price) = results.Price(CallMinusDelta, PutMinusDelta, r, T);

            // gamma call and put
            double CallGamma = (CallPlusDelta_Price -(2 * Call_Price) + CallMinusDelta_Price) / Math.Pow(DeltaS, 2);
            double PutGamma = (PutPlusDelta_Price -(2 * Put_Price) + PutMinusDelta_Price) / Math.Pow(DeltaS, 2);

            return (CallGamma, PutGamma);
        }

        public (double, double) Vega()
        {
            // ΔSigma
            double DeltaSigma = sigma * 0.01;

            // Sigma + ΔSigma and Sigma - ΔSigma for call and put
            (double[] CallPlusVega, double[] PutPlusVega) = montecarlo.Simulation(S, K, r, sigma * 1.01, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);
            (double[] CallMinusVega, double[] PutMinusVega) = montecarlo.Simulation(S, K, r, sigma * 0.99, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);

            (double CallPlusVega_Price, double PutPlusVega_Price) = results.Price(CallPlusVega, PutPlusVega, r, T);
            (double CallMinusVega_Price, double PutMinusVega_Price) = results.Price(CallMinusVega, PutMinusVega, r, T);

            // vega call and put
            double CallVega = (CallPlusVega_Price - CallMinusVega_Price) / (2 * DeltaSigma);
            double PutVega = (PutPlusVega_Price - PutMinusVega_Price) / (2 * DeltaSigma);

            return(CallVega, PutVega);
            
        }

        public (double, double) Theta()
        {
            // ΔT
            double DeltaT = T * 0.01;

            // T + ΔT and T for call and put
            (double[] CallPlusTheta, double[] PutPlusTheta) = montecarlo.Simulation(S, K, r, sigma, T * 1.01, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);
            (double[] Call, double[] Put) = montecarlo.Simulation(S, K, r, sigma, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);

            (double CallPlusTheta_Price, double PutPlusTheta_Price) = results.Price(CallPlusTheta, PutPlusTheta, r, T * 1.01);
            (double Call_Price, double Put_Price) = results.Price(Call, Put, r, T);

            // theta call and put
            double CallTheta = (CallPlusTheta_Price - Call_Price) / DeltaT;
            double PutTheta = (PutPlusTheta_Price - Put_Price) / DeltaT;

            return(CallTheta, PutTheta);
        }
        public (double, double) Rho()
        {
            // Δr
            double DeltaR = r * 0.01;

            // r + Δr and r - Δr for call and put
            (double[] CallPlusRho, double[] PutPlusRho) = montecarlo.Simulation(S, K, r * 1.01, sigma, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);
            (double[] CallMinusRho, double[] PutMinusRho) = montecarlo.Simulation(S, K, r * 0.99, sigma, T, steps, N, normal, Antithetic, ControlVariate, ParallelExecution, OptionClass, BarrierClass, P, H, LookBackClass);

            (double CallPlusRho_Price, double PutPlusRho_Price) = results.Price(CallPlusRho, PutPlusRho, r * 1.01, T);
            (double CallMinusRho_Price, double PutMinusRho_Price) = results.Price(CallMinusRho, PutMinusRho, r * 0.99, T);

            // rho call and put
            double CallRho = (CallPlusRho_Price - CallMinusRho_Price) / (2 * DeltaR);
            double PutRho = (PutPlusRho_Price - PutMinusRho_Price) / (2 * DeltaR);

            return(CallRho, PutRho);
        }
    }
}