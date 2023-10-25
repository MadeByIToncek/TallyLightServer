cat runtimes.txt | while read p 
do
  dotnet publish -c Release -r $p --self-contained false -p:PublishReadyToRun=true
  find bin/Release/net7.0/$p/publish -printf "%P\n" -type f -o -type l -o -type d | tar -czf out/$p.tar.gz --no-recursion -C bin/Release/net7.0/$p/publish -T -
done