$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $projectRoot
try {
    [xml]$xml = Get-Content -LiteralPath 'Assembly-CSharp.csproj' -Raw
    foreach ($node in @($xml.SelectNodes('//Compile | //ProjectReference | //Analyzer'))) { $node.ParentNode.RemoveChild($node) | Out-Null }
    foreach ($node in @($xml.SelectNodes('//Reference'))) {
        if ($node.Include -eq 'Afterlife.Devlog.Day01') { $node.ParentNode.RemoveChild($node) | Out-Null }
    }
    foreach ($node in $xml.SelectNodes('//AssemblyName')) { $node.InnerText='Afterlife.Devlog.Day01' }
    foreach ($node in $xml.SelectNodes('//BaseIntermediateOutputPath | //IntermediateOutputPath')) { $node.InnerText='Temp/obj/Threshold/Day01Preview/' }
    foreach ($node in $xml.SelectNodes('//OutputPath')) { $node.InnerText='Temp/bin/Threshold/Day01Preview/' }
    $properties=$xml.CreateElement('PropertyGroup')
    foreach ($pair in @(@('SkipResolvePackageAssets','true'),@('GenerateDependencyFile','false'))) {
        $node=$xml.CreateElement($pair[0]); $node.InnerText=$pair[1]; $properties.AppendChild($node) | Out-Null
    }
    $xml.Project.AppendChild($properties) | Out-Null
    $items=$xml.CreateElement('ItemGroup')
    $compile=$xml.CreateElement('Compile'); $compile.SetAttribute('Include','Assets/Devlog/Youtube/Day01/Runtime/*.cs'); $items.AppendChild($compile) | Out-Null
    foreach ($dll in Get-ChildItem 'Library/ScriptAssemblies/*.dll') {
        if ($dll.BaseName -eq 'Afterlife.Devlog.Day01' -or $dll.BaseName -eq 'Assembly-CSharp' -or $dll.BaseName -eq 'Assembly-CSharp-Editor') { continue }
        if ($xml.SelectNodes('//Reference') | Where-Object { $_.Include -eq $dll.BaseName }) { continue }
        $node=$xml.CreateElement('Reference'); $node.SetAttribute('Include',$dll.BaseName)
        $hint=$xml.CreateElement('HintPath'); $hint.InnerText=$dll.FullName; $node.AppendChild($hint) | Out-Null; $items.AppendChild($node) | Out-Null
    }
    $xml.Project.AppendChild($items) | Out-Null
    $project=Join-Path $projectRoot 'Afterlife.Day01Preview.ThresholdValidation.csproj'
    $xml.Save($project)
    dotnet build $project --no-restore --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'Day01 preview compilation failed' }
} finally { Pop-Location }
