# Important note regarding this fork

This tool was made by https://github.com/shairai/  
**Originally made for TFS 2013/2015, I've used it on Azure Devops Server 2022 as well, hence I will add the moniker "ADOCleaner" or "Azure Devops Server Cleaner"**  
The author's readme is below.

---


# TFS / Azure Devops Server Cleaner
![ScreenShot](/HelpImages/tfs-cleaner-logo.jpg)

TFS Cleaner is an administrator tool for TFS that provides utility for cleaning Workspaces, Shelves, Source Control, Test Attachments and Builds.


## Installation

The project was built using Visual Studio 2013. Clone, compile and you're done!

## Usage

### Workspace and Shelves
This section allows to query for Workspaces that weren’t changed in the past 30 days (you can change the Max Days) and shows Shelves that are older than 30 Days. For each Workspace item you can see the mappings.

You can search for a specific owner, and once the search is complete you can Copy the details to the Clipboard so you can send it to the owner to check if it's safe to delete.

![ScreenShot](/HelpImages/1.jpg)

### Source Control
For many of my customers, Source Control is responsible for a huge DB Size, due to the fact that customers check-in not just source files but also binary files.

When a binary is replaced with another version, a new revision is created and basically doubles the size for each revision.

The Source Control section allows you to easily browse Source Tree and see each file and folder size, this works per folder you open so you don’t have to wait until the entire tree size is calculated (can takes hours).

For each file and folder you can see the revisions and also copy the item details so you can check with the item owner if the item is needed and if not you can destroy the item.

You can also filter to display just deleted items and use the destroy button to completely remove those items from Source Control.

![ScreenShot](/HelpImages/2.jpg)

### Test Attachment Cleaner
Same as MS Test Attachment Cleaner but with UI Smile you can specify what file extensions to search and limit the search for specific work item states.

Of course you can define date range and file attachment size.

![ScreenShot](/HelpImages/3.jpg)

### Builds
The builds section allows you to search for builds based on their status, you can search for deleted builds and destroy full build definition with all his children.

![ScreenShot](/HelpImages/4.jpg)

## Contributing

1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request :D

## History

11/12/2014 - Added Initial Project
04/05/2015 - Updates for TFS 2013/2015

## Credits

Developed by Shai Raiten - http://blogs.microsoft.co.il/shair
