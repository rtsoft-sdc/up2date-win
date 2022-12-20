---
title: "Quick Start with UP2DATE agent for Windows"
permalink: /docs/install
excerpt: "Quick-Start Guide."
toc: true
---

UP2DATE agent for Windows can be installed to any PC running Windows 10, 11 either 32-bits or 64-bits.

**Note:** You need administrative rights to install it and switch management console to administrative mode 
{: .notice--warning}

## Installing using chocolatey

**Note:** This is the recommended way.
{: .notice--info}

1. Install `choco` if you haven't already:

    **Note:** See [detailed guide](https://docs.chocolatey.org/en-us/choco/setup): 
    {: .notice--info}

      ```powershell
      Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
      ```

1. ...

    ![image-center]({{ "/assets/images/install-choco.png" | relative_url }}){: .align-center}

1. Check status

    ![image-center]({{ "/assets/images/install-reboot.png" | relative_url }}){: .align-center}

1. Proceed to registration

    ![image-center]({{ "/assets/images/install-choco-register.png" | relative_url }}){: .align-center}


## MSI installer

...

## Register in the cloud

  ![image-right]({{ "/assets/images/install-register.png" | relative_url }}){: .align-right}
  ![image-center]({{ "/assets/images/install-token.png" | relative_url }}){: .align-center}
  ![image-center]({{ "/assets/images/install-register-pending.png" | relative_url }}){: .align-center}
  ![image-center]({{ "/assets/images/install-register-ok.png" | relative_url }}){: .align-center}
  ![image-center]({{ "/assets/images/install-register-status.png" | relative_url }}){: .align-center}
