using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

namespace TECH_ASM_LS1.Runner
{
    class Program
    {
        static int Main(string[] args)
        {
            switch (args[0])
            {
                case "list":
                    {
                        var custs = new Framework().GetCustomers();
                        foreach (var cust in custs)
                        {
                            System.Console.Out.WriteLine(cust.Item2);
                        }
                        return 0;
                    }
                case "export":
                    {
                        var custName = string.Join(" ", args.Skip(1));
                        if (custName == "all")
                        {
                            var allCusts = new Framework().GetCustomers();
                            foreach (var cust in allCusts)
                            {
                                var newArgs = cust.Item2.Split(' ').ToList();
                                newArgs.Insert(0, "export");
                                Main(newArgs.ToArray());
                            }
                            return 0;
                        }

                        // Create a safe directory for the Entity Manager sandbox
                        var appBase = new DirectoryInfo($"{Environment.GetEnvironmentVariable("TEMP")}\\{Guid.NewGuid()}");
                        appBase.Create();
                        foreach (var filename in new string[] { "TECH-ASM-LS1.dll", "EntMgr.dll", "EntMgr-Interfaces.dll" })
                            File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}\\{filename}", $"{appBase.FullName}\\{filename}");

                        // Create a sandbox to semi-safely execute Entity Manager code
                        var appDomainSetup = new AppDomainSetup()
                        {
                            ApplicationBase = appBase.FullName,
                        };
                        var grantSet = new PermissionSet(PermissionState.None);
                        // Guru code can only run, it can't do anything involving the filesystem,
                        // network, or UI, among other things. By extension, it hits other limits,
                        // such as reflection being limited to things it already knows about, 
                        // because otherwise it could access PII in directory names. It can't, 
                        // for example, check if an assembly named "System.Web" is loaded and 
                        // reach inside for HttpContext.Current, since it doesn't have a direct
                        // reference to System.Web. Just as a *completely not contrived* example.
                        grantSet.AddPermission(
                            new SecurityPermission(SecurityPermissionFlag.Execution));
                        // However, because our framework code is strong-name signed, we can
                        // grant it full trust in the sandbox so it can make its own security
                        // assertions and do things Guru code isn't allowed to do.
                        var appDomain = AppDomain.CreateDomain("Sandbox",
                            null, appDomainSetup, grantSet,
                            typeof(Framework).Assembly.Evidence.GetHostEvidence<StrongName>());
                        var framework = (Framework)Activator.CreateInstanceFrom(appDomain,
                            "TECH-ASM-LS1.dll", typeof(Framework).FullName).Unwrap();

                        // Get the customer from the command line
                        var custs = framework.GetCustomers();
                        var custGuid = custs.SingleOrDefault(
                            c => c.Item2.ToLowerInvariant() == custName.ToLowerInvariant()).Item1;

                        // Write the EDF for the customer
                        Console.Write(framework.GetEDFForEntity(custGuid));

                        // Product EDF streams are written last; roll them up from invoices.
                        var productGuids = new List<Guid>();

                        foreach (var invoiceId in framework.GetRelatedEntities(custGuid, "CUSI"))
                        {
                            // Write the EDF for the top-level invoice...
                            Console.Write(framework.GetEDFForEntity(invoiceId));

                            foreach (var invoiceLineId in framework.GetRelatedEntities(invoiceId, "INVI"))
                            {
                                // ...then the EDF for each item...
                                Console.Write(framework.GetEDFForEntity(invoiceLineId));

                                foreach (var productId in framework.GetRelatedEntities(invoiceLineId, "ITMP"))
                                {
                                    // ...and save the Product ID for later.
                                    if (!productGuids.Contains(productId)) productGuids.Add(productId);
                                }
                            }
                        }

                        // Finally, write the EDF for all products.
                        foreach (var productId in productGuids)
                        {
                            Console.Write(framework.GetEDFForEntity(productId));
                        }

                        return 0;
                    }
                default:
                    return int.MinValue;
            }
        }
    }
}
