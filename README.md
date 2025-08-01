[![Build Status](https://aelassas.visualstudio.com/wexflow/_apis/build/status/aelassas.wexflow?branchName=main)](https://aelassas.visualstudio.com/wexflow/_build/latest?definitionId=3&branchName=main) [![Nuget](https://img.shields.io/nuget/dt/Wexflow)](https://www.nuget.org/packages/Wexflow/) [![](https://raw.githubusercontent.com/aelassas/wexflow/refs/heads/loc/badge.svg)](https://github.com/aelassas/wexflow/actions/workflows/loc.yml) [![](https://img.shields.io/badge/docs-wiki-brightgreen)](https://github.com/aelassas/wexflow/wiki) [![NuGet](https://img.shields.io/nuget/v/Wexflow.svg)](https://www.nuget.org/packages/Wexflow/)

<!--
[![](https://img.shields.io/badge/docs-wiki-brightgreen)](https://github.com/aelassas/wexflow/wiki)
[![](https://raw.githubusercontent.com/aelassas/wexflow/refs/heads/loc/badge.svg)](https://github.com/aelassas/wexflow/actions/workflows/loc.yml)
[![Latest release](https://img.shields.io/github/v/release/aelassas/wexflow?label=Release&logo=github)](https://github.com/aelassas/wexflow/releases/latest)
-->

[![](https://wexflow.github.io/content/cover-small.png)](https://wexflow.github.io/)

## Wexflow

Wexflow is a cross-platform workflow automation engine designed to streamline and automate recurring tasks. It comes with a powerful workflow server, a modern admin panel for managing workflows, and supports both sequential and flowchart-based workflows.

With over 100 built-in activities, Wexflow supports a wide range of tasks out of the box—from file operations and system processes to scripting, networking, and more. You can also extend its capabilities by creating custom activities or integrating with external systems via the Wexflow API.

Wexflow targets both developers and technical users who need automation (file ops, tasks, scheduling, alerts, etc.). Wexflow is more like a task automation and scheduling platform. It focuses on automating technical jobs like moving or uploading files, sending emails, running scripts, or scheduling batch processes. It's great when you want to automate system tasks easily without writing much code. For more complex scenarios, you can create your own custom activities, install them, and use them within Wexflow to extend its capabilities.

Whether you're automating simple scheduled jobs or orchestrating complex business processes, Wexflow offers a flexible, extensible, and developer-friendly solution.

## Quick Links

- [Download Latest Release](https://github.com/aelassas/wexflow/releases/latest)
- [Install Guide](https://github.com/aelassas/wexflow/wiki/Installing)
- [Official Docker Hub Image](https://hub.docker.com/r/aelassas/wexflow)
- [Getting Started](https://github.com/aelassas/wexflow/wiki/Getting-Started)
- [Configuration Guide](https://github.com/aelassas/wexflow/wiki/Configuration)
- [REST API Reference](https://github.com/aelassas/wexflow/wiki/RESTful-API)
- [Built-in Tasks](https://github.com/aelassas/wexflow/wiki/Tasks)
- [Custom Tasks](https://github.com/aelassas/wexflow/wiki/Custom-Tasks)
- [Run From Source](https://github.com/aelassas/wexflow/wiki/Run-From-Source)
- [Fork, Customize, and Sync](https://github.com/aelassas/wexflow/wiki/Fork,-Customize,-and-Sync)

## Quick Start

You can run Wexflow using Docker from the official image on Docker Hub:
```bash
docker run -d -p 8000:8000 --name wexflow aelassas/wexflow:latest
```

Then access the Wexflow Admin Panel at: http://localhost:8000
- **Username:** `admin`  
- **Password:** `wexflow2018`

For full Docker usage and options, see the [Docker Hub page](https://hub.docker.com/r/aelassas/wexflow).

<!--
## Admin Panel Login

Once Wexflow is installed, you can access the admin panel at:

- **URL:** http://localhost:8000/  
- **Username:** `admin`  
- **Password:** `wexflow2018`

**Important:** For your security, change the default password after your first login.
-->

## Is Wexflow a Business Process Management Solution?

Wexflow is primarily a workflow automation engine, not a full BPM suite. Wexflow does not natively support BPMN, human workflows, or built-in user forms. So if your business processes require a lot of human interaction, approvals, or business rule evaluation, Wexflow would be limited.

Wexflow excels at automating technical tasks, such as moving or transforming files, uploading to FTP/SFTP, running scripts (PowerShell, Bash, Python, etc.), scheduling and chaining tasks, triggering workflows by events, manual input, cron or watchfolders, designing flows visually (Designer UI), integrating with APIs and databases, supporting conditional logic (if/else, switch, while).

You can use Wexflow if your processes are mostly system-based, such as back-office automation (file syncing, reporting, monitoring), ETL pipelines, DevOps or IT operations automation or API integrations between systems.

## How Does Wexflow Compare?

| Feature / Tool         | **Wexflow**            | Zapier         | Power Automate   | n8n             | Apache Airflow   |
|------------------------|------------------------|----------------|------------------|------------------|------------------|
| **Open Source**        | ✅ Yes                 | ❌ No          | ❌ No            | ✅ Yes          | ✅ Yes           |
| **Self-Hosted**        | ✅ Yes                 | ❌ No          | ✅ (Premium)     | ✅ Yes          | ✅ Yes           |
| **Visual Designer**    | ✅ Built-in (Web)      | ✅ Yes         | ✅ Yes           | ✅ Yes          | 🟡 Limited (via plugins) |
| **Custom Task Support**| ✅ C# tasks, scripts   | ❌ No          | 🟡 Limited       | ✅ JS functions | ✅ Python         |
| **Execution Graph**    | ✅ Yes (Flowchart)     | ❌ No          | ❌ No            | 🟡 Simple logic | ✅ DAGs          |
| **Trigger Types**      | Cron, Events, Watchers | App events     | App events       | Cron, Webhooks  | Cron, DAG Triggers |
| **Offline Usage**      | ✅ Yes                 | ❌ No          | ❌ No            | ✅ Yes          | ✅ Yes           |
| **Best For**           | Devs & Sysadmins       | Non-tech users | Business users   | Devs & startups | Data engineers   |

Wexflow gives you full control, extensibility, and offline capability with no vendor lock-in.

## Features

### Workflow Engine
* Cross-platform workflow server
* Supports sequential, flowchart, and approval workflows
* Cron-based scheduling
* 100+ built-in activities
* 6+ database engines supported
* Extensible architecture for custom activities
* [Push Notifications via SSE](https://github.com/aelassas/wexflow/wiki/Workflow-Notifications-via-SSE): Get real-time workflow job updates without polling
* Asynchronous workflow execution for improved concurrency and performance

### UI & Visualization
* Powerful web dashboard
* Visual workflow designer with drag & drop interface
* Real-time workflow statistics and monitoring
* Extensive logging for transparency and debugging

### Multi-Platform Support
* Native Android app
* Responsive web interface

### Internationalization & APIs
* Multiple language support (English, French, Danish)
* RESTful API for integration with external systems
* [REST API Clients](https://github.com/aelassas/wexflow/wiki/RESTful-API#sample-clients): Official examples for popular languages (C#, JS, PHP, Python, Go, Rust, Ruby, Java, C++)
* Extensible with Custom Activities via NuGet

### Security & Performance
* [Secure against XSS, XST, CSRF, and MITM](https://github.com/aelassas/wexflow/wiki/Wexflow-Security)
* Docker support for easy deployment
* Error monitoring

### Deployment & Compatibility
* Runs on macOS, Linux, Windows, and Docker

## Support

If this project helped you, saved you time, or inspired you in any way, please consider supporting its future growth and maintenance. You can show your support by starring the repository (it helps increase visibility and shows your appreciation), sharing the project (recommend it to colleagues, communities, or on social media), or making a donation (if you'd like to financially support the development) via [GitHub Sponsors](https://github.com/sponsors/aelassas) (one-time or monthly), [PayPal](https://www.paypal.me/aelassaspp), or [Buy Me a Coffee](https://www.buymeacoffee.com/aelassas). Open-source software requires time, effort, and resources to maintain—your support helps keep this project alive, up-to-date, and accessible to everyone. Every contribution, big or small, makes a difference and motivates continued work on features, bug fixes, and new ideas.

<!--<a href="https://github.com/sponsors/aelassas"><img src="https://aelassas.github.io/content/github-sponsor-button.png" alt="GitHub" width="210"></a>-->
<a href="https://www.paypal.me/aelassaspp"><img src="https://aelassas.github.io/content/paypal-button-v2.png" alt="PayPal" width="208"></a>
<a href="https://www.buymeacoffee.com/aelassas"><img src="https://aelassas.github.io/content/bmc-button.png" alt="Buy Me A Coffee" width="160"></a>

<!--
## Website Source Code (wexflow.github.io)

The source code for the official Wexflow website is available here:

[https://github.com/wexflow/wexflow.github.io](https://github.com/wexflow/wexflow.github.io)

It features a clean landing page with multilingual support, dark mode, and SEO optimizations to help it reach users in different languages and regions.

The codebase follows the Separation of Concerns (SoC) principle, with a modular and maintainable architecture that aligns with the Single Responsibility Principle (SRP), modularity, and modern frontend best practices. It uses GitHub Actions for automatic builds and deployments.

⚡ **Ultra-fast performance**

The website loads in under 1.5 seconds on slow 4G with **0ms blocking**, **0 layout shift**, and a blazing **Speed Index of 0.8**.

Feel free to explore the code, suggest improvements, or use it as a template for your own landing page.
-->

## Documentation

1. [Install Guide](https://github.com/aelassas/wexflow/wiki/Installing)  
   1. [Windows (.NET 4.8 - Legacy)](https://github.com/aelassas/wexflow/wiki/Installing#windows-net-48---legacy)  
   1. [Windows (.NET 9.0+ - Stable)](https://github.com/aelassas/wexflow/wiki/Installing#windows-net-90---stable)  
      1. [Accessing the Admin Panel](https://github.com/aelassas/wexflow/wiki/Installing#accessing-the-admin-panel)  
      1. [Configuration](https://github.com/aelassas/wexflow/wiki/Installing#configuration)  
      1. [Running Wexflow as a Windows Service using NSSM](https://github.com/aelassas/wexflow/wiki/Installing#running-wexflow-as-a-windows-service-using-nssm)  
      1. [Deleting the Wexflow Windows Service](https://github.com/aelassas/wexflow/wiki/Installing#deleting-the-wexflow-windows-service)  
   1. [Linux (.NET 9.0+ - Stable)](https://github.com/aelassas/wexflow/wiki/Installing#linux-net-90---stable)  
      1. [Installing the Admin Panel on NGINX](https://github.com/aelassas/wexflow/wiki/Installing#installing-the-admin-panel-on-nginx)  
      1. [Installing the Admin Panel on Apache2](https://github.com/aelassas/wexflow/wiki/Installing#installing-the-admin-panel-on-apache2)  
      1. [MongoDB Configuration](https://github.com/aelassas/wexflow/wiki/Installing#mongodb-configuration)  
      1. [Updating Wexflow](https://github.com/aelassas/wexflow/wiki/Installing#updating-wexflow)  
   1. [macOS (.NET 9.0+ - Stable)](https://github.com/aelassas/wexflow/wiki/Installing#macos-net-90---stable)  
   1. [Installing the Admin Panel on a Web Server](https://github.com/aelassas/wexflow/wiki/Installing#installing-the-admin-panel-on-a-web-server)  
      1. [.NET 4.8 - Legacy](https://github.com/aelassas/wexflow/wiki/Installing#net-48---legacy)  
      1. [.NET 9.0+ - Stable](https://github.com/aelassas/wexflow/wiki/Installing#net-90---stable)
1. [HTTPS/SSL](https://github.com/aelassas/wexflow/wiki/SSL)
1. [Screenshots](https://github.com/aelassas/wexflow/wiki/Screenshots)  
1. [Docker](https://github.com/aelassas/wexflow/wiki/Docker)  
1. [Configuration Guide](https://github.com/aelassas/wexflow/wiki/Configuration)  
   1. [Wexflow Server](https://github.com/aelassas/wexflow/wiki/Configuration#wexflow-server-configuration)  
   1. [Wexflow.xml](https://github.com/aelassas/wexflow/wiki/Configuration#wexflowxml-configuration-file)  
   1. [Admin Panel](https://github.com/aelassas/wexflow/wiki/Configuration#admin-panel-configuration)  
   1. [Authentication](https://github.com/aelassas/wexflow/wiki/Wexflow-Security)
1. [Persistence Providers](https://github.com/aelassas/wexflow/wiki/Persistence-Providers)  
1. [Getting Started](https://github.com/aelassas/wexflow/wiki/Getting-Started)  
1. [Android App](https://github.com/aelassas/wexflow/wiki/Android-App)  
1. [Local Variables](https://github.com/aelassas/wexflow/wiki/Local-Variables)  
1. [Global Variables](https://github.com/aelassas/wexflow/wiki/Global-Variables)  
1. [REST Variables](https://github.com/aelassas/wexflow/wiki/REST-Variables)  
1. [Functions](https://github.com/aelassas/wexflow/wiki/Functions)  
1. [Cron Scheduling](https://github.com/aelassas/wexflow/wiki/Cron-Scheduling)
1. [Command Line Interface (CLI)](https://github.com/aelassas/wexflow/wiki/Command-Line-Client)  
1. [REST API Reference](https://github.com/aelassas/wexflow/wiki/RESTful-API)  
   1. [Introduction](https://github.com/aelassas/wexflow/wiki/RESTful-API#introduction)
   1. [JWT Authentication](https://github.com/aelassas/wexflow/wiki/RESTful-API#jwt-authentication)
   1. [Sample Clients](https://github.com/aelassas/wexflow/wiki/RESTful-API#sample-clients)
      1. [C# Client](https://github.com/aelassas/wexflow/wiki/C%23-Client)  
      1. [JavaScript Client](https://github.com/aelassas/wexflow/wiki/JavaScript-Client)  
      1. [PHP Client](https://github.com/aelassas/wexflow/wiki/PHP-client)
      1. [Python Client](https://github.com/aelassas/wexflow/wiki/Python-Client)
      1. [Go Client](https://github.com/aelassas/wexflow/wiki/Go-Client)
      1. [Rust Client](https://github.com/aelassas/wexflow/wiki/Rust-Client)
      1. [Ruby Client](https://github.com/aelassas/wexflow/wiki/Ruby-Client)
      1. [Java Client](https://github.com/aelassas/wexflow/wiki/Java-Client)
      1. [C++ Client](https://github.com/aelassas/wexflow/wiki/CPP-Client)
   1. [Security Considerations](https://github.com/aelassas/wexflow/wiki/RESTful-API#security-considerations)
   1. [Swagger](https://github.com/aelassas/wexflow/wiki/RESTful-API#swagger)
   1. [Workflow Notifications via SSE](https://github.com/aelassas/wexflow/wiki/Workflow-Notifications-via-SSE)
      1. [C# SSE Client](https://github.com/aelassas/wexflow/wiki/C%23-SSE-Client)  
      1. [JavaScript SSE Client](https://github.com/aelassas/wexflow/wiki/JavaScript-SSE-Client)  
      1. [PHP SSE Client](https://github.com/aelassas/wexflow/wiki/PHP-SSE-Client)
      1. [Python SSE Client](https://github.com/aelassas/wexflow/wiki/Python-SSE-Client)
      1. [Go SSE Client](https://github.com/aelassas/wexflow/wiki/Go-SSE-Client)
      1. [Rust SSE Client](https://github.com/aelassas/wexflow/wiki/Rust-SSE-Client)
      1. [Ruby SSE Client](https://github.com/aelassas/wexflow/wiki/Ruby-SSE-Client)
      1. [Java SSE Client](https://github.com/aelassas/wexflow/wiki/Java-SSE-Client)
      1. [C++ SSE Client](https://github.com/aelassas/wexflow/wiki/CPP-SSE-Client)
   1. [Endpoints](https://github.com/aelassas/wexflow/wiki/RESTful-API#endpoints)
1. [Samples](https://github.com/aelassas/wexflow/wiki/Samples)  
   1. [Sequential workflows](https://github.com/aelassas/wexflow/wiki/Samples#sequential-workflows)  
   1. [Execution graph](https://github.com/aelassas/wexflow/wiki/Samples#execution-graph)  
   1. [Flowchart workflows](https://github.com/aelassas/wexflow/wiki/Samples#flowchart-workflows)  
      1. [If](https://github.com/aelassas/wexflow/wiki/Samples#if)  
      1. [While](https://github.com/aelassas/wexflow/wiki/Samples#while)  
      1. [Switch](https://github.com/aelassas/wexflow/wiki/Samples#switch)  
   1. [Approval workflows](https://github.com/aelassas/wexflow/wiki/Samples#approval-workflows)  
      1. [Simple approval workflow](https://github.com/aelassas/wexflow/wiki/Samples#simple-approval-workflow)  
      1. [OnRejected workflow event](https://github.com/aelassas/wexflow/wiki/Samples#onrejected-workflow-event)  
      1. [YouTube approval workflow](https://github.com/aelassas/wexflow/wiki/Samples#youtube-approval-workflow)  
      1. [Form submission approval workflow](https://github.com/aelassas/wexflow/wiki/Samples#form-submission-approval-workflow)  
   1. [Workflow events](https://github.com/aelassas/wexflow/wiki/Samples#workflow-events)  
1. [Logging](https://github.com/aelassas/wexflow/wiki/Logging)  
1. [Custom Tasks](https://github.com/aelassas/wexflow/wiki/Custom-Tasks)  
   1. [Introduction](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#introduction)
   1. [General](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#general)
       1. [Creating a Custom Task](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#creating-a-custom-task)
       1. [Wexflow Task Class Example](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#wexflow-task-class-example)
       1. [Task Status](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#task-status)
       1. [Settings](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#settings)
       1. [Loading Files](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#loading-files)
       1. [Loading Entities](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#loading-entities)
       1. [Need A Starting Point?](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#need-a-starting-point)
   1. [Installing Your Custom Task in Wexflow](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#installing-your-custom-task-in-wexflow)
       1. [.NET Framework 4.8 (Legacy Version)](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#net-framework-48-legacy-version)
       1. [.NET 8.0+ (Stable Version)](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#net-80-stable-version)
       1. [Referenced Assemblies](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#referenced-assemblies)
       1. [Updating a Custom Task](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#updating-a-custom-task)
       1. [Using Your Custom Task](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#using-your-custom-task)
   1. [Suspend/Resume](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#suspendresume)
   1. [Logging](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#logging)
   1. [Files](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#files)
   1. [Entities](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#entities)
   1. [Shared Memory](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#shared-memory)
   1. [Designer Integration](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#designer-integration)
       1. [Registering the Task](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#registering-the-task)
       1. [Adding Settings](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#adding-settings)
1. [Debugging](https://github.com/aelassas/wexflow/wiki/Custom-Tasks#debugging)
1. [Built-in Tasks](https://github.com/aelassas/wexflow/wiki/Tasks)  
   1. [File system tasks](https://github.com/aelassas/wexflow/wiki/Tasks#file-system-tasks)  
   1. [Encryption tasks](https://github.com/aelassas/wexflow/wiki/Tasks#encryption-tasks)  
   1. [Compression tasks](https://github.com/aelassas/wexflow/wiki/Tasks#compression-tasks)  
   1. [Iso tasks](https://github.com/aelassas/wexflow/wiki/Tasks#iso-tasks)  
   1. [Speech tasks](https://github.com/aelassas/wexflow/wiki/Tasks#speech-tasks)  
   1. [Hashing tasks](https://github.com/aelassas/wexflow/wiki/Tasks#hashing-tasks)  
   1. [Process tasks](https://github.com/aelassas/wexflow/wiki/Tasks#process-tasks)  
   1. [Network tasks](https://github.com/aelassas/wexflow/wiki/Tasks#network-tasks)  
   1. [XML tasks](https://github.com/aelassas/wexflow/wiki/Tasks#xml-tasks)  
   1. [SQL tasks](https://github.com/aelassas/wexflow/wiki/Tasks#sql-tasks)  
   1. [WMI tasks](https://github.com/aelassas/wexflow/wiki/Tasks#wmi-tasks)  
   1. [Image tasks](https://github.com/aelassas/wexflow/wiki/Tasks#image-tasks)  
   1. [Audio and video tasks](https://github.com/aelassas/wexflow/wiki/Tasks#audio-and-video-tasks)  
   1. [Email tasks](https://github.com/aelassas/wexflow/wiki/Tasks#email-tasks)  
   1. [Workflow tasks](https://github.com/aelassas/wexflow/wiki/Tasks#workflow-tasks)  
   1. [Social media tasks](https://github.com/aelassas/wexflow/wiki/Tasks#social-media-tasks)  
   1. [Waitable tasks](https://github.com/aelassas/wexflow/wiki/Tasks#waitable-tasks)  
   1. [Reporting tasks](https://github.com/aelassas/wexflow/wiki/Tasks#reporting-tasks)  
   1. [Web tasks](https://github.com/aelassas/wexflow/wiki/Tasks#web-tasks)  
   1. [Script tasks](https://github.com/aelassas/wexflow/wiki/Tasks#script-tasks)  
   1. [JSON and YAML tasks](https://github.com/aelassas/wexflow/wiki/Tasks#json-and-yaml-tasks)  
   1. [Entities tasks](https://github.com/aelassas/wexflow/wiki/Tasks#entities-tasks)  
   1. [Flowchart tasks](https://github.com/aelassas/wexflow/wiki/Tasks#flowchart-tasks)  
   1. [Approval tasks](https://github.com/aelassas/wexflow/wiki/Tasks#approval-tasks)  
   1. [Notification tasks](https://github.com/aelassas/wexflow/wiki/Tasks#notification-tasks)  
   1. [SMS tasks](https://github.com/aelassas/wexflow/wiki/Tasks#sms-tasks)  
1. [Run from Source](https://github.com/aelassas/wexflow/wiki/Run-From-Source)
1. [Fork, Customize, and Sync](https://github.com/aelassas/wexflow/wiki/Fork,-Customize,-and-Sync)

## Sponsors

<img alt="JetBrains" src="https://wexflow.github.io/content/jetbrains.png" width="120" />

## License

Wexflow is [MIT licensed](https://github.com/aelassas/wexflow/blob/main/LICENSE.txt).
