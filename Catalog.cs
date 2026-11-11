namespace AcousticVoid.Downloader;

public sealed record ToolGroup(string Name, string[] Urls);

public static class Catalog
{
    public static readonly IReadOnlyList<ToolGroup> Groups = new[]
    {
        new ToolGroup("Orbdiff", new[]
        {
            "https://github.com/Orbdiff/PrefetchView/releases/download/v1.6.8/pv++.exe",
            "https://github.com/Orbdiff/BAMReveal/releases/download/v1.3.1/BAMReveal.exe",
            "https://github.com/Orbdiff/MFT-HardLink/releases/download/v1.2/HardLink.exe",
            "https://github.com/Orbdiff/Fileless/releases/download/v1.3/fileless.exe",
            "https://github.com/Orbdiff/StringsParser/releases/download/v1.2.1b/stringsparser.1.2.1b.exe",
            "https://github.com/Orbdiff/AmcacheParser/releases/download/v1.0/AmcacheParser.exe",
            "https://github.com/Orbdiff/UserAssistView/releases/download/v1.0/UserAssistView.exe",
            "https://github.com/Orbdiff/USBDetector/releases/download/v1.1/USBDetector.exe",
            "https://github.com/Orbdiff/PFTrace/releases/download/v1.0.1/PFTrace.exe",
            "https://github.com/Orbdiff/JARParser/releases/download/v1.2/JARParser.exe",
            "https://github.com/Orbdiff/InjGen/releases/download/fork/InjGen.exe",
            "https://github.com/Orbdiff/WebHollowing/releases/download/v1.0/web.hollowing.exe",
            "https://github.com/Orbdiff/MFTParser/releases/download/v0.1/mftparser.exe",
            "https://github.com/Orbdiff/DPS-Analyzer/releases/download/v1.1/dpsanalyzer.exe",
            "https://github.com/Orbdiff/SSTool/releases/download/lol/SSTool.exe",
        }),
        new ToolGroup("Tonynoh", new[]
        {
            "https://github.com/MeowTonynoh/MeowClientFucker/releases/download/V1.1/MeowClientFucker.exe",
            "https://github.com/MeowTonynoh/MeowResolver/releases/download/v.1.1/MeowResolver.exe",
            "https://github.com/MeowTonynoh/MeowImportsChecker/releases/download/MeowImportsChecker/MeowImportsChecker.exe",
        }),
        new ToolGroup("Spokwn", new[]
        {
            "https://github.com/spokwn/JournalTrace/releases/latest/download/JournalTrace.exe",
            "https://github.com/spokwn/PathsParser/releases/download/v1.2/PathsParser.exe",
            "https://github.com/spokwn/BAM-parser/releases/latest/download/BAMParser.exe",
            "https://github.com/spokwn/prefetch-parser/releases/latest/download/PrefetchParser.exe",
            "https://github.com/spokwn/pcasvc-executed/releases/download/v0.8.7/PcaSvcExecuted.exe",
            "https://github.com/spokwn/ActivitiesCache-execution/releases/download/v0.6.5/ActivitiesCacheParser.exe",
            "https://github.com/spokwn/Replaceparser/releases/latest/download/Replaceparser.exe",
            "https://github.com/spokwn/BamDeletedKeys/releases/latest/download/BamDeletedKeys.exe",
            "https://github.com/spokwn/Tool/releases/latest/download/espouken.exe",
            "https://github.com/spokwn/KernelLiveDumpTool/releases/download/v1.1/KernelLiveDumpTool.exe",
            "https://github.com/spokwn/process-parser/releases/download/v0.5.5/ProcessParser.exe",
        }),
        new ToolGroup("Nirsoft", new[]
        {
            "https://www.nirsoft.net/utils/winprefetchview-x64.zip",
            "https://www.nirsoft.net/utils/lastactivityview.zip",
            "https://www.nirsoft.net/utils/executedprogramslist.zip",
            "https://www.nirsoft.net/utils/userassistview.zip",
            "https://www.nirsoft.net/utils/alternatestreamview-x64.zip",
            "https://www.nirsoft.net/utils/hashmyfiles-x64.zip",
            "https://www.nirsoft.net/utils/jumplistsview.zip",
            "https://www.nirsoft.net/utils/opensavefilesview-x64.zip",
            "https://www.nirsoft.net/utils/usbdeview-x64.zip",
            "https://www.nirsoft.net/utils/turnedontimesview.zip",
            "https://www.nirsoft.net/utils/regscanner-x64.zip",
            "https://www.nirsoft.net/utils/browserdownloadsview-x64.zip",
            "https://www.nirsoft.net/utils/clipboardic.zip",
            "https://www.nirsoft.net/utils/driverview-x64.zip",
            "https://www.nirsoft.net/utils/fileaccesserrorview-x64.zip",
            "https://www.nirsoft.net/utils/previousfilesrecovery-x64.zip",
            "https://www.nirsoft.net/utils/recentfilesview.zip",
            "https://www.nirsoft.net/utils/shellbagsview.zip",
            "https://www.nirsoft.net/utils/taskschedulerview-x64.zip",
            "https://www.nirsoft.net/utils/uninstallview-x64.zip",
            "https://www.nirsoft.net/utils/usbdrivelog.zip",
            "https://www.nirsoft.net/utils/networkusageview-x64.zip",
            "https://www.nirsoft.net/utils/ofview.zip",
            "https://www.nirsoft.net/utils/iehv.zip",
        }),
        new ToolGroup("Generic Tools", new[]
        {
            "https://github.com/winsiderss/si-builds/releases/download/4.0.26245.218/systeminformer-build-canary-setup.exe",
            "https://www.voidtools.com/Everything-1.4.1.1029.x64-Setup.exe",
            "https://github.com/Inkenal/RegistryScanner/releases/download/main/RegistryScanner.exe",
            "https://github.com/Inkenal/TaskParser/releases/download/main/VigilsTaskParser.exe",
            "https://github.com/horsicq/DIE-engine/releases/download/3.10/die_win64_portable_3.10_x64.zip",
            "https://github.com/deathmarine/Luyten/releases/download/v0.5.4_Rebuilt_with_Latest_depenencies/luyten-0.5.4.exe",
            "https://github.com/zedoonvm1/MarsPixelDumpAnalyzer/releases/download/Dev/MarsPixelDumpAnalyzer.exe",
            "https://mh-nexus.de/downloads/HxDPortableSetup.zip",
            "https://github.com/Sorted1/StormSS-Fuser-Finder/releases/download/Main/Storm.Fuser.Finder.zip",
            "https://github.com/Yamato-Security/hayabusa/releases/download/v4.1.0/hayabusa-4.1.0-win-x64.zip",
            "https://github.com/Col-E/Recaf/releases/download/2.21.14/recaf-2.21.14-J8-jar-with-dependencies.jar",
            "https://github.com/piespeas/MSC-Event-Viewer/releases/download/BETA/Event.Viewer.MSC.exe",
            "https://d1kpmuwb7gvu1i.cloudfront.net/Imgr/4.7.3.81%20Release/Exterro_FTK_Imager_%28x64%29-4.7.3.81.exe",
            "https://download.ccleaner.com/rcsetup154.exe",
            "https://www.winitor.com/tools/pestudio/current/pestudio.zip",
            "https://download.sysinternals.com/files/Strings.zip",
            "https://download.sysinternals.com/files/ProcessExplorer.zip",
            "https://download.sysinternals.com/files/Autoruns.zip",
            "https://download.sysinternals.com/files/ProcessMonitor.zip",
            "https://download.sysinternals.com/files/TCPView.zip",
            "https://github.com/Velocidex/WinPmem/releases/download/v4.0.rc1/go-winpmem_amd64_1.0-rc2_signed.exe",
            "https://github.com/ricniclac2/msc-browser-scanner/releases/download/Beta/MSC.Browser.Scanner.Setup.1.0.0.exe",
        }),
        new ToolGroup("Eric Zimmerman", new[]
        {
            "https://download.ericzimmermanstools.com/net/PECmd.zip",
            "https://download.ericzimmermanstools.com/net/MFTECmd.zip",
            "https://download.ericzimmermanstools.com/net/JLECmd.zip",
            "https://download.ericzimmermanstools.com/net/SrumECmd.zip",
            "https://download.ericzimmermanstools.com/net/bstrings.zip",
            "https://download.ericzimmermanstools.com/net/RecentFileCacheParser.zip",
            "https://download.ericzimmermanstools.com/net/JumpListExplorer.zip",
            "https://download.ericzimmermanstools.com/net/RegistryExplorer.zip",
            "https://download.ericzimmermanstools.com/net/ShellBagsExplorer.zip",
            "https://download.ericzimmermanstools.com/net/TimelineExplorer.zip",
            "https://builds.dotnet.microsoft.com/dotnet/Sdk/9.0.308/dotnet-sdk-9.0.308-win-x64.exe",
            "https://download.ericzimmermanstools.com/net/SBECmd.zip",
            "https://download.ericzimmermanstools.com/net/WxTCmd.zip",
            "https://download.ericzimmermanstools.com/net/AmcacheParser.zip",
        }),
        new ToolGroup("Detect", new[]
        {
            "https://detect.ac/tool/ToolsDownloader++",
        }),
    };
}
