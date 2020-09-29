# Bug-Tracker
My Bug Tracker web app is a ASP.NET Framework 4.7 side project. It's primary function is to keep track of software bugs/issues during development. For more information, please check the wikipedia page for a bug tracker with this [link](https://en.wikipedia.org/wiki/Bug_tracking_system)
## Installation
Download Visual Studio using this [link](https://visualstudio.microsoft.com/downloads/) to run the project. The Community is the free version.
Download Zip from this GitHub and extract it into a folder. The files you need are in the folders Bug Tracker and Packages as shown below. 
<img src="Screenshots/Zip.png" height="300">
<br>
Install and run Visual Studio. Once that done, you should be on Get Started page. Select Open a Local Folder option to find the extracted files. 
<br>
<img src="Screenshots/Step 1.png" height="300">
<br>
Double click the extracted folder, which was named Bug-Tracker-Master for me.
<br>
<img src="Screenshots/Step 2.png" height="300"> 
<br>
Once you're on this page, with the packages. Click Select Folder at the bottom right.
<br>
<img src="Screenshots/Step 3.png" height="300">
<br>
Once it's all loaded. Click on View and select Solution Explorer to find all the files.
<br>
<img src="Screenshots/Step 4.png" height="300">
<br>
Double Click on the solution. It's named Bug Tracker.sln for me.
<br>
<img src="Screenshots/Step 5.png" height="300">
<br>
After this, you should be all set to run this application. To run select IIS Express to run it on your browser.
<br>
<img src="Screenshots/Step 6.png" height="300">

## Features
### Account Creation and Login
<p>You create/register an account, and reset your password via email in case you forget</p>
<p float="left">
  <img src="Bug-Tracker Screenshot/Home/Register.png" height="300">
  <img src="Bug-Tracker Screenshot/Home/ForgetPassword.png" height="300">
</p>
<p>You can login with a created account or login using a Guest Account with varying privilege status</p>
<p float="left">
  <img src="Bug-Tracker Screenshot/Home/Login.png" height="300">
  <img src="Bug-Tracker Screenshot/Home/Guest Login.png" height="300">
</p>

### Projects
<p>The Project page has a table that contains of your account's project(s). This means that no matter what account privilege/status you have(Manager, Admin, Submitter, or Developer), as long as you are a part of the project currently the project will show up. Certain actions on this page are restricted based of account privilege/status like quiting and deleting the project.</p>
<p float="left">
  <img src="Bug-Tracker Screenshot/User/Project.png" height="400">
</p>
<p>The Edit Project page is only accessible to Manager and Admins. This page allows you to edit the project's information such as the details, users, and tickets.</p>
<p float="left">
  <img src="Bug-Tracker Screenshot/User/Edit Project User.png" height="300">
  <img src="Bug-Tracker Screenshot/User/Edit Project Ticket.png" height="300">
</p>

### Tickets
<p>The Ticket page has a table that contains all the tickets from your account's project(s). This means that tickets from different projects displayed here.</p>
<p float="left">
  <img src="Bug-Tracker Screenshot/User/Ticket.png" height="400">
</p>
<p>The Ticket Details page displays all the ticket's information(Ticket's Detail, Ticket History, Comments on the Ticket, and Attachments). On this page, you can add a comment/note to the ticket and upload an attachment(Images).</p>
<p float="left">
  <img src="Bug-Tracker Screenshot/User/Ticket Details.png" height="600">
</p>
