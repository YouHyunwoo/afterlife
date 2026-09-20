# 기존 Unity 생성 csproj는 변경하지 않고 프로토타입 소스만 DLL 참조로 컴파일한다.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$sourceProject = Join-Path $projectRoot 'Assembly-CSharp.csproj'
if (-not (Test-Path -LiteralPath $sourceProject)) { throw 'Unity에서 C# 프로젝트를 생성한 뒤 다시 실행하세요.' }
[xml]$projectXml = Get-Content -LiteralPath $sourceProject -Raw
foreach ($node in @($projectXml.SelectNodes('//Compile | //ProjectReference | //Analyzer'))) { $node.ParentNode.RemoveChild($node) | Out-Null }
foreach ($node in $projectXml.SelectNodes('//AssemblyName')) { $node.InnerText = 'Afterlife.Prototype.Validation' }
foreach ($node in $projectXml.SelectNodes('//BaseIntermediateOutputPath | //IntermediateOutputPath')) { $node.InnerText = 'Temp\obj\Afterlife.Prototype.Validation\' }
$propertyGroup = $projectXml.CreateElement('PropertyGroup')
foreach ($pair in @(@('SkipResolvePackageAssets','true'), @('GenerateDependencyFile','false'))) {
    $element = $projectXml.CreateElement($pair[0]); $element.InnerText = $pair[1]; $propertyGroup.AppendChild($element) | Out-Null
}
$projectXml.Project.AppendChild($propertyGroup) | Out-Null
$itemGroup = $projectXml.CreateElement('ItemGroup')
foreach ($pattern in @('Assets\_AfterlifePrototype\Runtime\*.cs','Assets\_AfterlifePrototype\Editor\*.cs','Assets\Afterlife\Modules\**\*.cs')) {
    $element = $projectXml.CreateElement('Compile'); $element.SetAttribute('Include',$pattern); $itemGroup.AppendChild($element) | Out-Null
}
foreach ($assembly in @('Unity.TextMeshPro','Moonstone.Core','Moonstone.Arc','Moonstone.D3.Domain','Unity.InputSystem','Unity.RenderPipelines.Core.Runtime','Unity.RenderPipelines.Universal.Runtime')) {
    if (-not ($projectXml.SelectNodes('//Reference') | Where-Object { $_.Include -eq $assembly })) {
        $reference = $projectXml.CreateElement('Reference'); $reference.SetAttribute('Include',$assembly)
        $hint = $projectXml.CreateElement('HintPath'); $hint.InnerText = "Library\ScriptAssemblies\$assembly.dll"
        $reference.AppendChild($hint) | Out-Null; $itemGroup.AppendChild($reference) | Out-Null
    }
}
$projectXml.Project.AppendChild($itemGroup) | Out-Null
$validationProject = Join-Path $projectRoot 'Afterlife.Prototype.Validation.csproj'
$projectXml.Save($validationProject)
Push-Location $projectRoot
try {
    dotnet build $validationProject --no-restore --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw "프로토타입 C# 빌드 실패: $LASTEXITCODE" }
} finally { Pop-Location }
