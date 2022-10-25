[![build](https://github.com/rtsoft-gmbh/up2date-win/actions/workflows/ci.yaml/badge.svg)](https://github.com/rtsoft-gmbh/up2date-win/actions/workflows/ci.yaml)
![license](https://img.shields.io/github/license/rtsoft-gmbh/up2date-win)
![GitHub release](https://img.shields.io/github/v/release/rtsoft-gmbh/up2date-win)
![GitHub Release Date](https://img.shields.io/github/release-date/rtsoft-gmbh/up2date-win)
![GitHub commit activity](https://img.shields.io/github/commit-activity/m/rtsoft-gmbh/up2date-win)
[![OpenSSF Scorecard]
(https://api.securityscorecards.dev/projects/github.com/rtsoft-gmbh/up2date-win/badge)]
(https://api.securityscorecards.dev/projects/github.com/rtsoft-gmbh/up2date-win)
# UP2DATE CLIENT for Windows

## INTRODUCTION

[RITMS UP2DATE](https://ritms.online) is a cloud ready solution for unified software and firmware management. Use this for implementing lifecycle management for the full stack of drivers and firmware of connected devices.

RITMS UP2DATE is based on open and worldwide adopted building blocks, the most important is [Eclipse Hawkbit](https://www.eclipse.org/ddi/) which provides open and flexible Direct Device Integration (DDI) API and Management API.

RITMS UP2DATE extends Eclipse Hawkbit API with zero-cost maintenance device provisioning based on X509 certificates. The Public Key Infrastructure deployed to cloud governs digital certificates to secure end-to-end communications. Devices are automatically provisioned to connect the update service in a secure way.

This UP2DATE CLIENT for Windows is a reference implementation of general purpose client service.

[see also up2date-cpp library which this application is based on](https://github.com/rtsoft-gmbh/up2date-cpp)

## GENERIC USE CASE

### Private MDM

Input: _any_ software deployment packaged im MSI form.

Task: assign deployment to all managed PCs.

Benefits:

* Silent install without UAC confirmation.
* Allows self signed deployments.
* Collect status from each managed PC.
* X509 certificate authorization.
* Single management point.

## QUICKSTART

1. Contact [RITMS UP2DATE](https://ritms.online) to get cloud service access and PC keys.
2. run `setup.exe` on each PC to be controlled from UP2DATE Cloud (admin privileges required)
3. Input unique one-time registration key requested after installation.
4. Log in [https://your.tenant.ritms.online](https://tenant.up2date.poc.ritms.online), upload a deployment, assign it to a distribution and drag-n-drop to dedicated PC(s)
5. Check a notification about installed deployment

## Build Wrapper for [up2date-cpp](https://github.com/rtsoft-gmbh/up2date-cpp) library
1. Clone [VCPKG repo](https://github.com/microsoft/vcpkg) in directory ../ from basic installation
2. run ../vcpkg/bootstrap-vcpkg.bat
3. Add Environment Variable %VCPKG_ROOT% with path to VCPKG storage
4. Install CMAKE 
5. Build via normal visual studio or msbuild compiler
