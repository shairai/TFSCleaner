# TFS Cleaner

TFS Cleaner is Administrator tool for TFS that allow cleaning Workspace, Shelves, Source Control, Test Attachment Cleaner and Builds.

## Installation

The project was built using Visual Studio 2013, clone, compile and you're done!

## Usage

### Workspace and Shelves
This sections allows to query for Workspaces that wasn’t changes in the past 30 days (you can change the Max Days) and shows Shelves that are older than 30 Days. For each Workspace item you can see the mappings.

You can specify that search for a specific owner, and once the search is complete you can Copy the details to Clipboard so you can send it the owner to check if it safe to delete.

![ScreenShot](/HelpImages/1.png)

### Source Control
For many of my customers Source Control is responsible for Huge DB Size, due to the fact that customers adds not just source files but also binary files.

When a binary is replace with another version a new revision is created and basically multiply the size for each revision.

Source Control sections allow to you to easily browse Source Tree and see each file and folder size, this works per folder you open so you don’t have to wait until the entire tree size is calculated (can takes hours).

For each file and folder you can see the revisions and also copy the item details so you can check with the item owner if the item is needed and if not you can destroy the item.

You can also filter to display just deleted items and use the destroy button the completely remove those items from Source Control.

![ScreenShot](/HelpImages/2.png)

### Test Attachment Cleaner
Same as MS Test Attachment Cleaner but with UI Smile you can specify what file extensions to search and limit the search for work item state.

Of course you can define date range and file attachment size.

![ScreenShot](/HelpImages/3.png)

### Builds
The builds section allow you to search for builds based on their status, you can search for deleted builds and destroy full build definition with all his children's.

![ScreenShot](/HelpImages/4.png)

## Contributing

1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request :D

## History

11/12/2014 - Add Initial Project

## Credits

Developed by Shai Raiten - http://blogs.microsoft.co.il/shair
