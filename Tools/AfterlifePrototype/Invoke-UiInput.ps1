param([int]$GameProcessId,[ValidateSet('Move','Click','Press','Release','MiddleClick','Drag','Escape','Wheel')][string]$Action,[int]$X=0,[int]$Y=0,[int]$ToX=0,[int]$ToY=0,[int]$Wheel=-120)
$ErrorActionPreference='Stop'
Add-Type @"
using System;using System.Runtime.InteropServices;
public static class PrototypeQaInput {
 [StructLayout(LayoutKind.Sequential)] public struct POINT {public int X;public int Y;}
 [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr value);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h,ref POINT p);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint x,uint y,uint data,UIntPtr extra);
 [DllImport("user32.dll")] public static extern void keybd_event(byte k,byte s,uint f,UIntPtr extra);
}
"@
[PrototypeQaInput]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))|Out-Null
$p=Get-Process -Id $GameProcessId
if($p.ProcessName -ne 'AfterlifePrototype' -or $p.MainWindowHandle -eq 0){throw 'Expected visible AfterlifePrototype window'}
[PrototypeQaInput]::SetForegroundWindow($p.MainWindowHandle)|Out-Null
$origin=New-Object PrototypeQaInput+POINT
[PrototypeQaInput]::ClientToScreen($p.MainWindowHandle,[ref]$origin)|Out-Null
if($Action -eq 'Escape'){[PrototypeQaInput]::keybd_event(27,1,0,[UIntPtr]::Zero);Start-Sleep -Milliseconds 60;[PrototypeQaInput]::keybd_event(27,1,2,[UIntPtr]::Zero);return}
[PrototypeQaInput]::SetCursorPos($origin.X+$X,$origin.Y+$Y)|Out-Null
[PrototypeQaInput]::mouse_event(1,1,0,0,[UIntPtr]::Zero)
Start-Sleep -Milliseconds 60
if($Action -eq 'Wheel'){[PrototypeQaInput]::mouse_event(0x800,0,0,[BitConverter]::ToUInt32([BitConverter]::GetBytes($Wheel),0),[UIntPtr]::Zero);return}
if($Action -eq 'Move'){return}
if($Action -eq 'Release'){[PrototypeQaInput]::mouse_event(4,0,0,0,[UIntPtr]::Zero);return}
if($Action -eq 'MiddleClick'){[PrototypeQaInput]::mouse_event(0x20,0,0,0,[UIntPtr]::Zero);Start-Sleep -Milliseconds 70;[PrototypeQaInput]::mouse_event(0x40,0,0,0,[UIntPtr]::Zero);return}
[PrototypeQaInput]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
if($Action -eq 'Press'){return}
if($Action -eq 'Drag'){for($i=1;$i -le 15;$i++){[PrototypeQaInput]::SetCursorPos($origin.X+$X+($ToX-$X)*$i/15,$origin.Y+$Y+($ToY-$Y)*$i/15)|Out-Null;[PrototypeQaInput]::mouse_event(1,1,0,0,[UIntPtr]::Zero);Start-Sleep -Milliseconds 80}}else{Start-Sleep -Milliseconds 70}
[PrototypeQaInput]::mouse_event(4,0,0,0,[UIntPtr]::Zero)


