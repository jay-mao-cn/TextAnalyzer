<h1>About</h1>
<p>Inspired by TextAnalysisTool.NET, implemented by Avalonia UI to support Windows/Mac/Linux. With some new faatures:</p>
<ol>
  <li>Chart viewer</li>
  <li>Text viewer window which can format json or EoL</li>
  <li>Filter with logical operations (&& or ||)</li>
  <li>Hide empty lines</li>
  <li>OS theme adaptive</li>
</ol>
<h1>Build</h1>
<p>Install dotnet 8.0 SDK.</p>
<h2>Windows</h2>
<p>Just use Visual Studio 2022 (or above) to open TextAnalyzer.sln and build.</p>
<h2>Mac</h2>
<ol>
  <li>Open the folder which contains TextAnalyzer.csproj in VS Code.</li>
  <li>Press Cmd+Shift+B or click "Terminal"->"Run Build Task...", and then select "build".</li>
  <li>Run "sh ./Scripts/build_mac_specific_code.sh" once in terminal.</li>
</ol>

<h1>Deployment</h1>
<h2>Windows</h2>
<p>Nothing special, just run the TextAnalyzer.exe.</p>
<p>You can also make it available in System context menu's "Open with" by "Choose another app".</p>
<h2>Mac</h2>
<ol>
  <li>In VS Code, click "Terminal"->"Run Task...", select "publish x64|arm64".</li>
  <li>Run "sh ./Scripts/package_mac_app.sh [x86_64|arm64]" in terminal.</li>
  <li>Drag & drop the "Text Analyzer.app" in the "bin" folder to System "Applications".</li>
  <li>The app will be available in the "Open With" context menu for text files.</li>
  <li>If you want to sign the app, run "sh ./Scripts/sign_mac_app.sh [x86_64|arm64]" (change the developer ID to your own).</li>
  <li>Note: If deployed the signed app to another Mac, it may still encounter security warnings, please follow system steps to allow the app to open.</li>
</ol>

<h1>Usage</h1>
<ol>
  <li>Open a text file by "Open with" or drag & drop to a running Text Analyzer app.</li>
  <li>Create some filters based on text or marker (Ctrl+1~9 or right click a text line to toggle the marker).</li>
  <li>You can toggle "Show Only Filtered Lines" (Ctrl+H) as needed.</li>
  <li>Press "a~z" or "F8" to locate next match of the corresponding filter.</li>
  <li>Move up/down the filters to change priority (drag & drop also works on Windows).</li>
  <li>Double click the text line to view in an independent window which supports "Find" and "Format" functions (context menu).</li>
  <li>Press "F5" to reload the text file.</li>
  <li>Save the filter ([Ctrl/Cmd]+Shift+S) for next time use.</li>
  <li>Check other usages from app menu or context menu.</li>
</ol>