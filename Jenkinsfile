node {
    stage 'Checkout'
    git url: 'https://github.com/Techbot121/DiscordBot'
    step([$class: 'GitHubSetCommitStatusBuilder'])
    
    if(isUnix())
    {
        stage 'Build'
        
        withEnv(['DOTNET=/usr/local/bin/dotnet']) // todo make this more flexible
        {
            
            sh 'screen -X -S "oldhal1320" quit &'
            sh '''cd src/DiscordBot/
                $DOTNET restore
                $DOTNET build
                BUILD_ID=dontKillMe
                screen -dmS "oldhal1320" $DOTNET run &''' // ditto
                                
            stage 'Post-Build'
            step([$class: 'GitHubCommitStatusSetter', statusResultSource: [$class: 'ConditionalStatusResultSource', results: []]])

        }
        
    }
    else
     {
        error 'NotImplemented'
    }
}