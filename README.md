# CamApi
C# API and examples for the Edgertronic camera

Download or clone to local machine (dotnet core 2.0 must be installed)</br>
&nbsp;&nbsp;run <code>dotnet build</code></br>
&nbsp;&nbsp;run <code>dotnet publish -cRelease -rwin10-x64</code></br>
</br>
Run <code>.\CamApiExample\bin\Release\netcoreapp2.0\win10-x64\CamApiExample.exe</code></br>
</br>Options (must have = with no spaces between option and value):</br>
&nbsp;&nbsp;<b>-a --Address</b>           Host name or IP # of camera (default 10.11.12.13)</br>
&nbsp;&nbsp;<b>-f --FavoritesTest</b>     1 to run favorites tests (deletes all saved favorites)</br>
&nbsp;&nbsp;<b>-c --CaptureTest</b>       1 to run capture tests (time consuming, writes to storage)</br>
&nbsp;&nbsp;<b>-m --MultiCaptureTest</b>  1 to run multi-capture tests (time consuming, writes to storage)</br>
&nbsp;&nbsp;<b>-h --Help</b>              1 to display this message</br>
