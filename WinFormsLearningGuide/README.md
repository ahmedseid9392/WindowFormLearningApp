# Windows Forms Learning & Reference Guide — Phase 4

A reading / learning / code-reference application for the WinForms toolbox
(not a designer). This package contains **Phase 1 (layout and navigation), Phase 2 (documentation content), Phase 3 (properties reference) and Phase 4 (events and methods)**.

## Run it

Requirements: Windows 10/11 and the .NET 10 SDK (https://dotnet.microsoft.com/download).

    cd WinFormsLearningGuide
    dotnet run

or open `WinFormsLearningGuide.csproj` in Visual Studio 2022/2026 and press F5.

Using .NET 8 or 9? Edit `<TargetFramework>` in the .csproj to `net8.0-windows` or `net9.0-windows`.

## What Phase 4 adds

Sections **4. Events Reference** and **5. Methods Reference** are now real on all 44 control pages
(108 documented events and 122 documented methods).

**Events** - for each event: when it is raised, the delegate type, the event-argument class with what its
members give you, a real-world use case, and a complete C# example (the subscription line, any setup it
needs, and the handler method). Covers every event shown in the quick-reference panel.

**Methods** - for each method: the full signature, description, every parameter with its type, the return
type and what it returns, a C# example and the expected effect. Includes the collection calls beginners use
all the time (`Items.Add`, `Controls.Add`, `Rows.Add`, `TabPages.Add`...).

**Inherited members** - shared events and methods are written once in `Data/Events/Inherited.json` and
`Data/Methods/Inherited.json` and shown as a table at the end of each page: 23 `Control` events and 16
`Control` methods; 10 events and 5 methods for toolbar items (`ToolStripItem`); `Disposed` and `Dispose()` for
non-visual components. A member documented on the control itself replaces the shared entry. In the inherited
tables the example is a one-line subscription or call (`ctl.Click += ...`).

Content lives in `Data/Events/*.events.json` and `Data/Methods/*.methods.json` (one file per category,
matched to controls by name). To change or add text, edit those files; no code changes needed. An entry that
names an unknown control stops the app at startup with a clear message.

Code added or changed: `EventDocumentation` (new Delegate and ArgumentsNote fields), `MethodDocumentation`,
`ParameterDocumentation`, `ControlMembers` (ControlEvents / ControlMethods), `ControlDocumentation`
(Events, InheritedEvents, Methods, InheritedMethods, MemberBase), `DocumentationService` (LoadEvents,
LoadMethods), `SectionContentBuilder` (BuildEvents, BuildMethods), `SearchService` (searches event and method
names) and `DocumentationSection.CurrentPhase = 4`. The existing content blocks and `SectionCard` are reused unchanged.

Section 6 (Complete C# Example) is still a placeholder (Phase 5).

## What Phase 3 adds

Section **3. Properties Reference** is now real on all 44 control pages (244 documented properties):

* **Table** (Property | Data Type | Description | Example) of the control's own key properties.
* **Property details** for each one: meaning, data type, default value, accepted values, practical usage,
  a C# example in a code block, and the expected effect.
* **Inherited properties**: the properties every control shares (Name, Text, BackColor, ForeColor, Enabled,
  Visible, Size, Location, Font, Dock, Anchor, Margin, Padding, TabIndex, TabStop, Tag, Controls) are written
  once in `Data/Properties/Inherited.json` and shown as a table on every control page. Items that live on a
  ToolStrip use a separate ToolStripItem set. A property documented on the control itself replaces the shared entry.

Content lives in `Data/Properties/*.properties.json` (one file per category, matched to controls by name) and
`Data/Properties/Inherited.json`. To change or add text, edit those files; no code changes needed. An entry that
names an unknown control stops the app at startup with a clear message.

Code added or changed: `PropertyDocumentation` (new `Usage` field), `ControlProperties`, `DocumentationService`
(loads the files), `SectionContentBuilder` (builds section 3), `ContentBlock` (new Title, Field, Code and Table
blocks) and `SectionCard` (draws them; it now caches its layout and only paints the visible blocks, so the long
properties card stays fast).

**Fix:** the Phase 2 zip did not compile because of an unescaped quote in `SectionContentBuilder.cs`
(the note under Related Controls). That line is corrected here.


## What Phase 2 adds

Real content for four of the eight sections on every one of the 44 control pages:

* **1. Overview** - summary, purpose, when to use it, real-world examples
* **2. Appearance** - description of the default look plus live illustrations: the real
  WinForms control is created and rendered to an image (and again with `Enabled = false`
  where that looks different). Where a control draws nothing itself (ToolTip, ErrorProvider,
  ContextMenuStrip) a clearly labelled *simulated* illustration is shown; components with no
  user interface (dialogs, Timer, BackgroundWorker, BindingSource, NotifyIcon) say so instead.
* **7. Common Mistakes** - typical beginner mistakes, each with the fix
* **8. Related Controls** - every related control with how it differs

Content lives in `Data/Content/*.content.json` (one file per category, matched to controls by
name). To change or add text, edit those files; no code changes needed. A content entry that
names an unknown control stops the app at startup with a clear message.

Code added: `ControlContent` model, `ContentBlock`, block-based `SectionCard`,
`ControlPreviewService` (renders the illustrations), `SectionContentBuilder`.

## What Phase 1 includes

* Three resizable panels: toolbox (left), documentation (center), quick reference (right)
* 44 controls in 6 categories, loaded from `Data/*.json`
* Toolbox: expandable categories, A–Z view, Recent, Favorites, live search filter
* Header with Favorite, Previous / Next; Alt+Left / Alt+Right also navigate
* Ctrl+F focuses the search box (Enter opens the first result, Esc clears)
* Quick reference: namespace, class, inheritance, key properties/events, related-control links, jump-to-section links
* Dark (default) and light themes, remembered between runs
* Progress counter, favorites and recents saved in `%AppData%\WinFormsLearningGuide\userdata.json`


## Next phases

~~2 documentation content~~ (done) · ~~3 properties~~ (done) · ~~4 events/methods~~ (done) · 5 code viewer · 6 playground · 7 search/progress polish · 8 testing
