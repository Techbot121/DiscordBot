node {
    stage 'Checkout'
    git url: 'https://github.com/Techbot121/DiscordBot'
    step([$class: 'GitHubSetCommitStatusBuilder'])
    
    if(isUnix())
    {
        stage 'Build'
        
        withEnv(['DOTNET=/usr/local/bin/dotnet']) // todo make this more flexible
        {
            
            sh 'screen -X -S "hal1320" quit &'
            sh '''cd ~/repos/DiscordBot/src/DiscordBot/
            	git pull
                $DOTNET restore
                $DOTNET build''' // ditto
                
            sh '''cd ~/repos/DiscordBot/src/DiscordBot/
                BUILD_ID=dontKillMe
                screen -dmS "hal1320" $DOTNET run &''' // ditto
                
            stage 'Post-Build'
            step([$class: 'GitHubCommitStatusSetter', statusResultSource: [$class: 'ConditionalStatusResultSource', results: []]])

        }
        
    }
    else
     {
        error 'NotImplemented'
    }
}