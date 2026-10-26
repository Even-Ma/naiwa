[CmdletBinding()]
param(
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packageDirectory = Join-Path $repoRoot 'dist\Naiwa'
$archivePath = Join-Path $repoRoot 'dist\Naiwa-Windows-x64.zip'
$testDirectory = Join-Path $repoRoot 'work\self-test'
$utf8 = New-Object System.Text.UTF8Encoding($true)

# Valid local assets are reused; a source-only checkout fetches the pinned files.
& (Join-Path $PSScriptRoot 'fetch-assets.ps1')

$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) {
    throw '未找到 Windows 的 C# 编译器。请在 Windows 10 / 11 上构建，并确认已安装 .NET Framework 4.x。'
}

$requiredFiles = @(
    'assets\素材.nwp', 'assets\原版笑声.wav', 'assets\naiwa.ico',
    'assets\actions\manifest.json', 'assets\actions\sheets\shufflestep.png',
    'assets\actions\sheets\headbob.png', 'assets\actions\sheets\holdlaugh.png',
    'src\Pet.manifest', 'LICENSE', 'THIRD_PARTY_NOTICES.md'
)
foreach ($relativePath in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $relativePath) -PathType Leaf)) {
        throw ('缺少构建文件：' + $relativePath + '。请保留完整项目文件。')
    }
}
$actionSourceDirectory = Join-Path $repoRoot 'assets\actions'
$actionManifest = Get-Content -LiteralPath (Join-Path $actionSourceDirectory 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($actionManifest.version -ne 1) { throw '玩梗动作 manifest.json 版本无效。' }
foreach ($actionId in @('shufflestep', 'headbob', 'holdlaugh')) {
    $matchesForId = @($actionManifest.actions | Where-Object { $_.id -ceq $actionId })
    if ($matchesForId.Count -ne 1 -or $matchesForId[0].sheet -cne ('sheets/' + $actionId + '.png')) {
        throw ('玩梗动作清单缺少或重复定义：' + $actionId)
    }
}
$actionSourceFiles = @(Get-ChildItem -LiteralPath $actionSourceDirectory -File -Recurse | Sort-Object FullName)
$sources = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Filter '*.cs' -File | Sort-Object Name | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) { throw 'src 文件夹中没有 C# 源代码。' }

[IO.Directory]::CreateDirectory($packageDirectory) | Out-Null
$executablePath = Join-Path $packageDirectory '奶蛙桌宠.exe'
$compilerArguments = @(
    '/nologo', '/utf8output', '/codepage:65001', '/target:winexe', '/optimize+', '/platform:x64',
    ('/win32icon:' + (Join-Path $repoRoot 'assets\naiwa.ico')),
    ('/win32manifest:' + (Join-Path $repoRoot 'src\Pet.manifest')),
    ('/out:' + $executablePath),
    '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll',
    '/reference:System.IO.Compression.dll', '/reference:System.IO.Compression.FileSystem.dll',
    '/reference:System.Web.Extensions.dll'
) + $sources

Write-Output '正在构建奶蛙桌宠 1.4.0（Windows x64）……'
& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) { throw ('编译失败，退出码：' + $LASTEXITCODE) }

Copy-Item -LiteralPath (Join-Path $repoRoot 'assets\素材.nwp') -Destination (Join-Path $packageDirectory '素材.nwp') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'assets\原版笑声.wav') -Destination (Join-Path $packageDirectory '原版笑声.wav') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $packageDirectory 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD_PARTY_NOTICES.md') -Destination (Join-Path $packageDirectory 'THIRD_PARTY_NOTICES.md') -Force
$packagedActionFiles = @()
foreach ($sourceFile in $actionSourceFiles) {
    $relativeActionPath = $sourceFile.FullName.Substring($actionSourceDirectory.Length + 1)
    $relativePackagePath = Join-Path 'actions' $relativeActionPath
    $destination = Join-Path $packageDirectory $relativePackagePath
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    Copy-Item -LiteralPath $sourceFile.FullName -Destination $destination -Force
    if ((Get-FileHash -LiteralPath $sourceFile.FullName -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash) {
        throw ('玩梗动作复制校验失败：' + $relativeActionPath)
    }
    $packagedActionFiles += $relativePackagePath
}

$usage = @'
奶蛙桌宠
版本：1.4.0

开始使用
1. 先把压缩包完整解压到任意文件夹。
2. 双击「奶蛙桌宠.exe」。奶蛙会出现在鼠标所在屏幕的右下角。
3. 请让程序、「素材.nwp」和「原版笑声.wav」一直放在同一文件夹。
   也请完整保留随程序附带的 actions 文件夹。

陪伴与互动
· 鼠标移到奶蛙身上，它会转眼看你，待机时会自然眨眼。
· 单击肚子有回弹；按住左键可拖动；双击可捧腹大笑。
· 笑倒后单击站起来。笑声默认关闭，可在右键菜单里开启。
· 右键奶蛙，选择「打开陪伴面板 / 专注计时」：可开始 25 分钟专注，
  可以暂停、继续、重置计时，也可以调整大小、笑声、置顶、全屏隐藏。
· 关闭陪伴面板后奶蛙仍会留在桌面；需要结束时，右键选择「退出奶蛙」。
  面板关闭时计时继续，到期会通过托盘提醒。切回「摸鱼」会暂停计时。
· 双击任务栏托盘中的奶蛙图标，可显示或恢复陪伴面板，奶蛙位置不变。
· 全屏隐藏默认开启；奶蛙所在屏幕的前台应用全屏时，它会暂时隐藏。

玩梗动作
· 面板中三个无描边动作按钮和右键菜单「玩梗动作」可选择「闪身步」、
  「摇头晃脑」和「憋笑」。动作播放完会自动回到待机。
· 可以重播或切换动作；「站起来」会停止当前动作。
  拖动、播放原版大笑或进入全屏隐藏时，也会打断当前玩梗动作。
· 新动作由 ImageGen 以原角色为参考制作独立姿态，借鉴闪身步、
  摇头晃脑等网络趣味动作和憋笑通用表情。资源随程序附带，运行离线。

找不到奶蛙
双击托盘中的奶蛙图标显示或恢复陪伴面板，再选「回到右下角」；也可
在奶蛙或托盘的右键菜单中选择「回到屏幕右下角」。再次运行程序仍会
让它回到鼠标所在屏幕的右下角。托盘图标可能收在任务栏的小箭头内。
仍处于全屏时，请退出全屏，或通过托盘右键菜单关闭「全屏时自动隐藏」。

设置与隐私
本程序离线运行，无需账号；没有自动更新、遥测或开机自启。
设置保存在 %LOCALAPPDATA%\NaiwaCompanion\settings.ini。
退出奶蛙后删除此文件，可恢复默认设置。
专注计时仅在本次运行中保留，退出程序会结束计时。

运行环境
Windows 10 / 11（64 位），.NET Framework 4.5 或更新版本。
通常无需另装依赖；无需管理员权限。

来源与使用范围
本项目是 yugh6/Naiwa 的非商业衍生版，保留上游形象、动画和原版笑声。
原项目：https://github.com/yugh6/Naiwa
上游作者：yugh6（抖音：握石竹，抖音号：45357131546）。
请保留 LICENSE 和 THIRD_PARTY_NOTICES.md，不要冒充原创或用于商业用途。
公开发布短视频等场景，请先按上游要求联系作者取得许可。
'@
[IO.File]::WriteAllText((Join-Path $packageDirectory '使用说明.txt'), $usage, $utf8)

if ($SelfTest) {
    [IO.Directory]::CreateDirectory($testDirectory) | Out-Null
    $testLog = Join-Path $testDirectory 'self-test.txt'
    if (Test-Path -LiteralPath $testLog) { Remove-Item -LiteralPath $testLog -Force }
    Write-Output '正在检查素材、动作、窗口交互与陪伴功能……'
    $testProcess = Start-Process -FilePath $executablePath -ArgumentList @('--self-test', ('"' + $testDirectory + '"')) -WorkingDirectory $packageDirectory -WindowStyle Hidden -PassThru
    if (-not $testProcess.WaitForExit(180000)) {
        Stop-Process -Id $testProcess.Id -Force -ErrorAction SilentlyContinue
        throw '自测超过 3 分钟，已结束测试进程。请查看 work\self-test。'
    }
    $testProcess.Refresh()
    if ((-not (Test-Path -LiteralPath $testLog)) -or $testProcess.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $testLog) { Get-Content -LiteralPath $testLog | Write-Output }
        throw '自测失败。请查看 work\self-test\self-test.txt。'
    }
    $testText = [IO.File]::ReadAllText($testLog)
    if ($testText -notmatch '(?m)^RESULT: PASS\s*$' -or $testText -match '(?m)^RESULT: FAIL') {
        Write-Output $testText
        throw '自测报告没有通过。'
    }
    Write-Output ('自测通过：' + $testLog)
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$packageFiles = @('奶蛙桌宠.exe', '素材.nwp', '原版笑声.wav', '使用说明.txt', 'LICENSE', 'THIRD_PARTY_NOTICES.md')
$packageFiles += $packagedActionFiles
$archiveStream = [IO.File]::Open($archivePath, [IO.FileMode]::Create, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $archive = New-Object IO.Compression.ZipArchive($archiveStream, [IO.Compression.ZipArchiveMode]::Create, $true, [Text.Encoding]::UTF8)
    try {
        foreach ($fileName in $packageFiles) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $packageDirectory $fileName), ('Naiwa/' + $fileName.Replace('\', '/')), [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose() }
} finally { $archiveStream.Dispose() }

$verificationArchive = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    if ($verificationArchive.Entries.Count -ne $packageFiles.Count) { throw '便携压缩包文件数量校验失败。' }
    foreach ($fileName in $packageFiles) {
        $entryName = 'Naiwa/' + $fileName.Replace('\', '/')
        $entry = $verificationArchive.GetEntry($entryName)
        if ($null -eq $entry -or $entry.Length -ne (Get-Item -LiteralPath (Join-Path $packageDirectory $fileName)).Length) {
            throw ('便携压缩包缺少或未完整写入：' + $entryName)
        }
    }
} finally { $verificationArchive.Dispose() }

Write-Output ('程序：' + $executablePath)
Write-Output ('便携压缩包：' + $archivePath)
