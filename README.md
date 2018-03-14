# CamApi
C# API and examples for the Edgertronic camera

Download or clone to local machine (dotnet core 2.0 must be installed)</br>
&nbsp;&nbsp;run <code>dotnet build</code></br>
&nbsp;&nbsp;run <code>dotnet publish -c Release -r win10-x64</code></br>
</br>
Run <code>.\CamApiExample\bin\Release\netcoreapp2.0\win10-x64\CamApiExample.exe</code></br>
</br>Options (must have = with no spaces between option and value):</br>
&nbsp;&nbsp;<b>-a --Address</b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Host name or IP # of camera (default 10.11.12.13)</br>
&nbsp;&nbsp;<b>-f --FavoritesTest</b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1 to run favorites tests (deletes all saved favorites)</br>
&nbsp;&nbsp;<b>-c --CaptureTest</b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1 to run capture tests (time consuming, writes to storage)</br>
&nbsp;&nbsp;<b>-m --MultiCaptureTest</b>&nbsp;&nbsp;&nbsp;1 to run multi-capture tests (time consuming, writes to storage)</br>
&nbsp;&nbsp;<b>-h --Help</b>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;1 to display this message</br>
