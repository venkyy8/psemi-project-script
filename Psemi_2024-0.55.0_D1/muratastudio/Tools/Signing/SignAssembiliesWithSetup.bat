echo on
call "%vs120comntools%vsvars32.bat"
set pw=p$c10011
set certPath=Cert\pSemiCorporationMP.pfx
rem set timestampServer=http://tsa.starfieldtech.com
set timestampServer=http://timestamp.comodoca.com/authenticode
set releasePath=..\..\Apps\muRata\bin\Release
set setupath=..\..\Solutions\muRata.Applications\muRataStudioSetup\Release

signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "muRataStudio" %releasePath%"\muRataStudio.exe"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "AdapterAccess" %releasePath%"\AdapterAccess.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "DeviceAccess" %releasePath%"\DeviceAccess.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "HardwareInterfaces" %releasePath%"\HardwareInterfaces.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "PluginFramework" %releasePath%"\PluginFramework.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "AdapterControl" %releasePath%"\Plugins\AdapterControl.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "MPQControl" %releasePath%"\Plugins\MPQControl.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "MPQChartControl" %releasePath%"\Plugins\MPQChartControl.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "MPQ7920Control" %releasePath%"\Plugins\MPQ7920Control.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "ARCxCCxxControl" %releasePath%"\Plugins\ARCxCCxxControl.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "DocumentViewerControl" %releasePath%"\Plugins\DocumentViewerControl.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "HelpViewerControl" %releasePath%"\Plugins\HelpViewerControl.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "RegisterControl" %releasePath%"\Plugins\RegisterControl.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "PE24103Control" %releasePath%"\Plugins\PE24103Control.dll"
signtool sign /t %timestampServer%  /f %certPath% /p %pw% /d "setup" %setupath%"\setup.exe"
pause