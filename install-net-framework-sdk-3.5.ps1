# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at
# 
#   http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied.  See the License for the
# specific language governing permissions and limitations
# under the License.
#
#Enforce TLS 1.2 as Microsoft is deprecating all old TLS versions
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;

if (-not (Test-Path dotnetfx35.exe)) {
    Write-Host "Downloading dotnetfx35.exe"
    Invoke-WebRequest -Uri "https://download.microsoft.com/download/2/0/E/20E90413-712F-438C-988E-FDAA79A8AC3D/dotnetfx35.exe" -OutFile dotnetfx35.exe
}

Write-Host "Running dotnetfx35.exe"
$process = Start-Process -FilePath dotnetfx35.exe -ArgumentList "/wait","/passive" -Wait -PassThru
if ($process.ExitCode -eq 0) {
    Write-Host "dotnetfx35 installed"
} else {
    Write-Host "dotnetfx35 installer returned exit code ${process.ExitCode}"
}

if (-not (Test-Path dotnetfx35client.exe)) {
    Write-host "Downloading dotnetfx35client.exe"
    Invoke-WebRequest -Uri "https://download.microsoft.com/download/c/d/c/cdc0f321-4f72-4a08-9bac-082f3692ecd9/DotNetFx35Client.exe" -OutFile dotnetfx35client.exe
}

Write-Host "Running dotnetfx35client.exe"
$process = Start-Process -FilePath dotnetfx35client.exe -ArgumentList "/quiet","/passive" -Wait -PassThru
if ($process.ExitCode -eq 0) {
    Write-Host "dotnetfx35client installed"
} else {
    Write-Host "dotnetfx35client installer returned exit code ${process.ExitCode}"
}
