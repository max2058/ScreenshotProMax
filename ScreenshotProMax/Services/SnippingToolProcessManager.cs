using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ScreenshotProMax.Services
{
    /// <summary>
    /// Verwaltet das Beenden und Überwachen von Windows Snipping Tool Prozessen
    /// </summary>
    public static class SnippingToolProcessManager
    {
        // Bekannte Namen von Snipping Tool Prozessen
        private static readonly string[] SnippingToolProcessNames = new[]
        {
            "SnippingTool",          // Klassisches Snipping Tool
            "ms-screenclip",         // Windows 10/11 Ausschneiden und Skizzieren
            "ScreenClippingHost",    // Windows 10/11 Screen Clipping Host
            "Microsoft.ScreenSketch", // Windows 10/11 Screenshot App
            "ScreenSketch",          // Alternative Name
            "WindowsInternal.ComposableShell.Experiences.TextInput.InputApp" // Manchmal verwendet
        };

        /// <summary>
        /// Beendet alle aktiven Snipping Tool Prozesse
        /// </summary>
        /// <returns>Anzahl der beendeten Prozesse</returns>
        public static int TerminateSnippingToolProcesses()
        {
            int terminatedCount = 0;
            
            try
            {
                foreach (var processName in SnippingToolProcessNames)
                {
                    try
                    {
                        var processes = Process.GetProcessesByName(processName);
                        foreach (var process in processes)
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"Beende Snipping Tool Prozess: {process.ProcessName} (PID: {process.Id})");
                                process.Kill();
                                process.WaitForExit(1000); // Warte max 1 Sekunde auf Beendigung
                                terminatedCount++;
                                System.Diagnostics.Debug.WriteLine($"Prozess erfolgreich beendet: {process.ProcessName}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Fehler beim Beenden von {process.ProcessName}: {ex.Message}");
                            }
                            finally
                            {
                                process.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fehler beim Suchen nach Prozess {processName}: {ex.Message}");
                    }
                }
                
                if (terminatedCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Insgesamt {terminatedCount} Snipping Tool Prozesse beendet");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Allgemeiner Fehler beim Beenden der Snipping Tool Prozesse: {ex.Message}");
            }
            
            return terminatedCount;
        }

        /// <summary>
        /// Überprüft ob Snipping Tool Prozesse aktiv sind
        /// </summary>
        /// <returns>True wenn Snipping Tool Prozesse gefunden wurden</returns>
        public static bool IsSnippingToolRunning()
        {
            try
            {
                foreach (var processName in SnippingToolProcessNames)
                {
                    try
                    {
                        var processes = Process.GetProcessesByName(processName);
                        if (processes.Length > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Aktiver Snipping Tool Prozess gefunden: {processName} ({processes.Length} Instanzen)");
                            
                            // Ressourcen freigeben
                            foreach (var process in processes)
                            {
                                process.Dispose();
                            }
                            
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fehler beim Prüfen von Prozess {processName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Überprüfen der Snipping Tool Prozesse: {ex.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// Überwacht kontinuierlich Snipping Tool Prozesse und beendet sie automatisch
        /// </summary>
        /// <param name="intervalMs">Überwachungsintervall in Millisekunden (Standard: 500ms)</param>
        /// <returns>Task für die asynchrone Überwachung</returns>
        public static async Task StartContinuousMonitoring(int intervalMs = 500)
        {
            System.Diagnostics.Debug.WriteLine("Starte kontinuierliche Snipping Tool Prozess-Überwachung");
            
            try
            {
                while (true)
                {
                    if (IsSnippingToolRunning())
                    {
                        var terminated = TerminateSnippingToolProcesses();
                        if (terminated > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Automatische Überwachung: {terminated} Snipping Tool Prozesse beendet");
                        }
                    }
                    
                    await Task.Delay(intervalMs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei kontinuierlicher Überwachung: {ex.Message}");
            }
        }

        /// <summary>
        /// Beendet spezifische Windows Screenshot-Services (falls sie als Service laufen)
        /// </summary>
        /// <returns>True wenn erfolgreich</returns>
        public static bool DisableSnippingToolServices()
        {
            try
            {
                var serviceNames = new[]
                {
                    "ClipSVC", // Clipboard Service (kann Screenshots auslösen)
                    "UserDataSvc", // User Data Service (kann mit Screenshot-Apps interagieren)
                };

                // Hinweis: Service-Manipulation erfordert oft Admin-Rechte
                // Hier implementieren wir eine sanfte Variante
                
                foreach (var serviceName in serviceNames)
                {
                    try
                    {
                        // Versuche Service-Prozesse zu finden und zu beenden (falls sie Snipping Tool-bezogen sind)
                        var processes = Process.GetProcessesByName(serviceName);
                        foreach (var process in processes)
                        {
                            try
                            {
                                // Nur beenden wenn es eindeutig ein Screenshot-bezogener Prozess ist
                                var processModules = process.Modules.Cast<ProcessModule>();
                                bool isScreenshotRelated = processModules.Any(m => 
                                    m.ModuleName.Contains("snip", StringComparison.OrdinalIgnoreCase) ||
                                    m.ModuleName.Contains("clip", StringComparison.OrdinalIgnoreCase) ||
                                    m.ModuleName.Contains("sketch", StringComparison.OrdinalIgnoreCase));

                                if (isScreenshotRelated)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Beende screenshot-bezogenen Service-Prozess: {serviceName}");
                                    process.Kill();
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Fehler beim Analysieren von Service {serviceName}: {ex.Message}");
                            }
                            finally
                            {
                                process.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Service {serviceName} konnte nicht verarbeitet werden: {ex.Message}");
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Deaktivieren der Screenshot-Services: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Erweiterte Überwachung die auch auf neue Prozess-Starts reagiert
        /// </summary>
        public static void StartAdvancedMonitoring()
        {
            try
            {
                // Task für kontinuierliche Überwachung starten (nicht blockierend)
                Task.Run(async () =>
                {
                    await StartContinuousMonitoring(300); // Alle 300ms prüfen für schnelle Reaktion
                });

                System.Diagnostics.Debug.WriteLine("Erweiterte Snipping Tool Überwachung gestartet");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Starten der erweiterten Überwachung: {ex.Message}");
            }
        }

        /// <summary>
        /// Stoppe alle Snipping Tool Prozesse und verhindere Neustart für kurze Zeit
        /// </summary>
        /// <returns>Task für die asynchrone Ausführung</returns>
        public static async Task AggressiveSnippingToolSuppression()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starte aggressive Snipping Tool Unterdrückung");
                
                // Mehrere Runden der Prozess-Beendigung
                for (int round = 0; round < 3; round++)
                {
                    var terminated = TerminateSnippingToolProcesses();
                    if (terminated > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Runde {round + 1}: {terminated} Prozesse beendet");
                    }
                    
                    await Task.Delay(100); // Kurze Pause zwischen den Runden
                }
                
                // Überwache für die nächsten 2 Sekunden intensiv
                var endTime = DateTime.Now.AddSeconds(2);
                while (DateTime.Now < endTime)
                {
                    if (IsSnippingToolRunning())
                    {
                        TerminateSnippingToolProcesses();
                    }
                    await Task.Delay(50); // Sehr kurzes Intervall für aggressive Überwachung
                }
                
                System.Diagnostics.Debug.WriteLine("Aggressive Snipping Tool Unterdrückung abgeschlossen");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler bei aggressiver Unterdrückung: {ex.Message}");
            }
        }
    }
}