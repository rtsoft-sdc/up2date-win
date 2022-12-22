---
title: "Quick Start with UP2DATE Agent"
permalink: /install
excerpt: "Quick-Start Guide."
toc: true
---

UP2DATE agent for Windows is the reference implementation of Eclipse hawkBit™ DDI client.
A PC running this agent gets remote control on software update capabilities and cloud will take care of facing IT security threats.

Obviously, a PC is not a first grade citizen of Internet of Things, but with this you definitely are getting a simplest way to experience how IoT solution works in production.

As hawkBit™ designers say:
  > Updating the software for an entire car may differ from updating the firmware of a single sensor with regard to the connectivity of the device to the cloud and also to the complexity of the software package update process on the device. However, the process of rolling out the software, e.g. uploading an artifact to the repository, assigning it to eligible devices, managing the roll out campaign for a large number of devices, orchestrating content delivery networks to distribute the package, monitoring and reporting the progress of the roll-out and last but not least requirements regarding security and reliability are quite similar.

**Note:** You need local administrative privileges to install the Agent and switch Management Console to Administrative mode 
{: .notice--warning}

## MSI installer

The Agent can be installed to any PC running Windows 10, 11 either 32-bits or 64-bits. Just download appropriate `up2date.windows-x64.zip` or `up2date.windows-x86.zip` from assets of the <a href="https://github.com/rtsoft-gmbh/up2date-win/releases">latest release</a>, extract and run `Setup.exe`

**Note:** The installer will fetch all necessary dependencies but you may be asked to reboot the system to finish software setup of .Net framework or Visual C++ Redistributable.
{: .notice--warning}

Then proceed to [registration step](#register-in-the-cloud).

## Installing using chocolatey

**Note:** This is the recommended way.
{: .notice--info}

Alternatively you can use [Chocolatey](https://docs.chocolatey.org/en-us/choco/setup) - a software management solution independent from Microsoft where UP2DATE for Windows is also [hosted](https://community.chocolatey.org/packages/up2date).

With Chocolatey installed and UP2DATe Agent running on your PC you will be able remotely and securely deliver any [chocolatey package](https://community.chocolatey.org/packages) in addition to MSI packaged software. 

**Note:** And this now is the only way to self-update UP2DATe for Windows - just drag-n-drop signed choco package of new UP2DATE Agent release to your device in hawkBit™ Web UI
{: .notice--success}

1. Install [choco](https://docs.chocolatey.org/en-us/choco/setup) if you haven't already.

1. Instal up2date agent from PowerShell (run is as an [administrator](https://learn.microsoft.com/en-us/powershell/scripting/windows-powershell/starting-windows-powershell?view=powershell-7.3#with-administrative-privileges-run-as-administrator)):

      ```powershell
      choco install up2date
      ```

    ![image-center]({{ "/assets/images/install-choco.png" | relative_url }}){: .align-center}

1. Check status

    ![image-center]({{ "/assets/images/install-reboot.png" | relative_url }}){: .align-center}

1. Proceed to registration

    ![image-center]({{ "/assets/images/install-choco-register.png" | relative_url }}){: .align-center}


## Register in the cloud

  Agent Installer starts Administration Console immediately and first check on a clean install rises an alert asking you to proceed with registration in Cloud Service (by default it is RITMS UP2DATE):

  ![image-right]({{ "/assets/images/install-register.png" | relative_url }}){: .align-right}

  Only thing necessary to register and provision the Agent in RITMS UP2DATE cloud service is One-Time Token (OTT), which you receive from Service Administrator.
  The token contains Controller Identifier for you device which will appear in hawkBit Web UI. Administrator can issue OTT with predefined Controller ID or use Machine GUI shown in Authorization Dialog. 
  
  Last case obviously assume a step when you send Machine GUID to Administrator and receive OTT in change.
  {: .notice--info}

  ![image-center]({{ "/assets/images/install-token.png" | relative_url }}){: .align-center}

  When OTT is put to Authorization Dialog and Authorize button is pressed the Agent starts to be automatically provisioning in the cloud:

  ![image-center]({{ "/assets/images/install-register-pending.png" | relative_url }}){: .align-center}

  If everything goes well you'll see a success message:

  ![image-center]({{ "/assets/images/install-register-ok.png" | relative_url }}){: .align-center}

  ... and UP2Date Console window footer shows you a Controller ID and Tenant Web UI where device is registered:

  ![image-center]({{ "/assets/images/install-register-status.png" | relative_url }}){: .align-center}
