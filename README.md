[![build](https://github.com/rtsoft-gmbh/up2date-win/actions/workflows/ci.yaml/badge.svg)](https://github.com/rtsoft-gmbh/up2date-win/actions/workflows/ci.yaml)
![license](https://img.shields.io/github/license/rtsoft-gmbh/up2date-win)
![GitHub release](https://img.shields.io/github/v/release/rtsoft-gmbh/up2date-win)
![GitHub Release Date](https://img.shields.io/github/release-date/rtsoft-gmbh/up2date-win)
![GitHub commit activity](https://img.shields.io/github/commit-activity/m/rtsoft-gmbh/up2date-win)

[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/rtsoft-gmbh/up2date-win/badge)](https://api.securityscorecards.dev/projects/github.com/rtsoft-gmbh/up2date-win)

# UP2DATE CLIENT for Windows

## QUICKSTART

1. Contact [RITMS UP2DATE](https://ritms.online) to get cloud service access and PC keys.
2. Proceed with [Documentation containing Installation manual](https://rtsoft-gmbh.github.io/up2date-win). 

or read documentation on how to setup management in your own Eclipse hawkBit deployment.

## INTRODUCTION

[RITMS UP2DATE](https://up2date.ritms.online) is a cloud ready solution for unified software and firmware management. Use this for implementing lifecycle management for the full stack of drivers and firmware of connected devices.

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
