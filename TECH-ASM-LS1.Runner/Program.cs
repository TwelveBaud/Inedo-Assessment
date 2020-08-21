using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

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
                        foreach (var filename in new string[] { "TECH-ASM-LS1.dll", "EntMgr.dll", "EntMgr-Interfaces.dll", "System.Web.dll" })
                            File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}\\{filename}", $"{appBase.FullName}\\{filename}");

                        // Create a sandbox to semi-safely execute Entity Manager code
                        var appDomainSetup = new AppDomainSetup()
                        {
                            ApplicationBase = appBase.FullName,
                        };
                        var grantSet = new PermissionSet(PermissionState.None);
                        grantSet.AddPermission(
                            new SecurityPermission(SecurityPermissionFlag.Execution));
                        //HACK: Allow Products code to investigate whether or not its in a """secure"""
                        // environment by lowering actual security. *sigh*
                        // This shouldn't result in information disclosure, but it's theoretically
                        // possible for the Guru to smuggle information out through fabricated
                        // entity GUIDs if he knows that this is a thing.
                        grantSet.AddPermission(
                            new FileIOPermission(PermissionState.None)
                            { AllLocalFiles = FileIOPermissionAccess.PathDiscovery });
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
