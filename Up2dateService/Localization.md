# Localization notes
This document provides instructions to maintain localization of the RITMS UP2DATE for Windows agent.

## Scope
The localization scope within the RITMS UP2DATE for Windows solution is limited to Up2DateConsole – the only UI project in the solution.

## Concept
The console UI project is built on WPF, so the localization concept is based on the recommended for WPF approach where the localizable resources is extracted from the main assembly into so-called satellite DLLs, one per language/culture.
On run time the localization support framework determines the current language and tries to find needed localizable resource (e.g., string) in the corresponding satellite DLL. If there’s no such DLL or no such resource in the satellite DLL, the resource is being get from the “neutral” satellite DLL generated automatically when the project is built.
> Important: this approach assumes that all localizable elements and strings are defined in XAMLs (as separate resources or within UI elements – doesn’t matter).

## Development environment
It is assumed you have Visual Studio (Community 2019 at least) installed on your computer.
To make localization maintenance possible you should setup `locbaml` tool:
1.	Copy to your computer `locbaml.exe` tool from here:
https://github.com/vchaplinski/locbaml/tree/master/bin/Release
2.	Set environment variable `%locbaml%` to the full path of the `locbaml.exe` you just copied.

## Maintenance
`Up2DateConsole` project is already appropriately configured for localization. So, this section describes routine actions needed in different development scenarios. You have to execute these actions to make sure your localization is up-to-date.

### Scenario #1. Any changes made in XAMLs
Open Develop Command Prompt, go to Localization folder and execute the following script with the language specified as parameter:
`Update.cmd ru-RU`

Open `Up2dateConsole_ru-RU.csv` file in any text editor convenient to edit csv tables.
Note! If you use MS Excel then make sure all columns are treated as Text to avoid problems with unexpected conversions.

The meaning of the columns:
1.	BAML Name. The name of the BAML resource with respect to the source language satellite assembly.
2.	Resource Key. The localized resource identifier.
3.	Category. The value type.
4.	Readability. Whether the value can be read by a localizer.
5.	Modifiability. Whether the value can be modified by a localizer.
6.	Comments. Additional description of the value to help determine how a value is localized.
7.	Value. Text value translated to the desired culture.
8.	Original Value. The current text value in default language to be translated to the desired culture.
9.	Update Tag. If not empty – indicates that Value field has to be updated according to (translated from) the Original Value field.

Look at the 9th column. If you see any rows with the `new` or `changed` tags in this column, it means that resources represented in these rows need your attention. Normally rows with `Readability` `Modifiability` set to `False` can be ignored. Just clear the `Update Tag` value for these rows.
For other rows read `Original Value`, if it is localizable translate it and put the translated text into the `Value` field, then clear the `Update Tag` field. 
After this process all the rows in the file should have empty `Update Tag`.
Save the file.
In order to test your localization, re-build the project and run it in the appropriate locale environment. 

### Scenario #2. Localization to another language is requested
1.	Create additional empty resource file for the new language:
In VisualStudio add “Resource File” to Up2dateConsole/Properties with the corresponding name, for instance `Resources.fr-FR.resx`
2.  Open Develop Command Prompt, go to Localization folder and execute the following script with the language specified as parameter:
`Update.cmd fr-FR`
3.  In Visual Studio add to the project (to the Localization folder) the file Up2dateConsole_fr-FR.csv created on the previous step.
4.	Translate all localizable texts as described in Scenario #1.
5.	Open Build Events page on the Up2dateConsole project Properties screen.
6.  Extend `Post-build event command line` to cover additional language

### Scenario #3. New localizable UI project is created
1.	Insert the following lines into the yourproject.csproj file:
<PropertyGroup>
    <UICulture>en-US</UICulture>
</PropertyGroup>
2.	In the file `AssemblyInfo.cs` uncomment or add the following line:
[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]
3.	Create `Localization` folder at the project root and put `CheckAndGenerate.cmd` file into it. You can copy this file from another localizable project, for instance from Up2dateConsole.
4.	For every supported non-default language perform steps described in Scenario #2 replacing everywhere `Up2DateConsole` with `yourproject`
5.	In Setup project add `Project output` of type `Localized resources` (in addition to `Primary output`)

## More to read
https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/globalization-and-localization?view=netframeworkdesktop-4.8
https://putridparrot.com/blog/localizing-a-wpf-application-using-locbaml/
