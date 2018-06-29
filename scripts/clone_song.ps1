<#
Example script for cloning a song a bunch for testing large data sets.
Careful with this.
#>
for($i=1;$i -le 2000;$i++)
{
    copy-item Believer -destination $i -Recurse
    (Get-Content $i\info.json).replace('Believer', "Believer$i") | Set-Content $i\info.json    
}