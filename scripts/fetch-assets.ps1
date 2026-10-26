[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetDirectory = Join-Path $repoRoot 'assets'
[IO.Directory]::CreateDirectory($assetDirectory) | Out-Null
$commit = '72c763fe1349fb08a06f976e1709f4f5f80e09c7'
$baseUrl = 'https://raw.githubusercontent.com/yugh6/Naiwa/' + $commit + '/'
$assetList = @(
    @{ Name = '素材.nwp'; Source = '%E7%B4%A0%E6%9D%90.nwp'; Sha256 = 'a9c7795974d860da0c82b45237456698640ee8d4252b5db37503383d23fa46ae' },
    @{ Name = '原版笑声.wav'; Source = '%E5%8E%9F%E7%89%88%E7%AC%91%E5%A3%B0.wav'; Sha256 = 'e808f6f932abfb81e7104eb1c586fe6c828bda5db4ef62e52e0638a64a01fdfa' },
    @{ Name = 'naiwa.ico'; Source = '%E6%BA%90%E7%A0%81/naiwa.ico'; Sha256 = 'a4f75cefe859f51f39cf84588b9aca701bd10da684ae3bbf88db6d3dfcff8f60' }
)

# Windows PowerShell 5.1 may otherwise negotiate an obsolete TLS version.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

foreach ($asset in $assetList) {
    $destination = Join-Path $assetDirectory $asset.Name
    if (Test-Path -LiteralPath $destination -PathType Leaf) {
        $existingHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        if ($existingHash -eq $asset.Sha256) {
            Write-Output ('素材校验通过：' + $asset.Name)
            continue
        }
        Write-Output ('素材校验不符，重新获取：' + $asset.Name)
    }
    $temporaryPath = Join-Path $assetDirectory ('.download-' + [Guid]::NewGuid().ToString('N'))
    try {
        Write-Output ('正在从固定上游版本获取：' + $asset.Name)
        Invoke-WebRequest -Uri ($baseUrl + $asset.Source) -OutFile $temporaryPath -UseBasicParsing -TimeoutSec 180
        $downloadHash = (Get-FileHash -LiteralPath $temporaryPath -Algorithm SHA256).Hash
        if ($downloadHash -ne $asset.Sha256) {
            throw ('SHA-256 校验不符，未采用下载内容：' + $asset.Name)
        }
        # A failed or interrupted download never replaces the current asset.
        Move-Item -LiteralPath $temporaryPath -Destination $destination -Force
        Write-Output ('素材已获取并验证：' + $asset.Name)
    }
    catch {
        throw ('无法获取 ' + $asset.Name + '。请检查网络后重试；也可从完整源码包恢复 assets 文件夹。' + [Environment]::NewLine + $_.Exception.Message)
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
    }
}
