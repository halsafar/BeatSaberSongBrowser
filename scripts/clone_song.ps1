<#
Example script for cloning a song a bunch for testing large data sets.
Careful with this.
#>
for($i=1;$i -le 2000;$i++)
{
    copy-item Sunset -destination $i -Recurse
    (Get-Content $i\info.json).replace('Sunset', "Sunset$i") | Set-Content $i\info.json    
    (Get-Content $i\info.json).replace('from Deemo', "SunsetAuthor$i") | Set-Content $i\info.json    
}