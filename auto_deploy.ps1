$source = "bin\Release\net6.0\ArfSchedule1.dll"
$dest = "E:\Games\Steam\steamapps\common\Schedule I\Mods\ArfSchedule1.dll"

Write-Host "[AutoDeploy] Esperando a que el proceso 'Schedule I' se cierre para copiar la DLL..."
while (Get-Process -Name "Schedule I" -ErrorAction SilentlyContinue) {
    Start-Sleep -Seconds 1
}

Start-Sleep -Milliseconds 500
try {
    Copy-Item -Path $source -Destination $dest -Force
    Write-Host "[AutoDeploy] ¡DLL copiada con éxito a Mods\ArfSchedule1.dll!"
} catch {
    Write-Host "[AutoDeploy] Error al copiar: $_"
}
