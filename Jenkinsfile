node {
    stage 'Checkout'
    git url: 'https://github.com/Techbot121/DiscordBot'
    step([$class: 'GitHubSetCommitStatusBuilder'])
    
    if(isUnix())
    {
        stage 'Build'
        
        withEnv(["PATH+DNX=/var/lib/jenkins/.dnx/runtimes/dnx-mono.1.0.0-rc1-update2/bin"]) // todo make this more flexible
        {
            sleep 30 //so hal can post the github changes
            
            sh 'screen -X -S "hal1320" quit &'
            sh '''cd ~/repos/DiscordBot/src/DiscordBot/
            	git pull
                dnu restore
                dnu build --configuration RELEASE''' // ditto
                
            sh '''cd ~/repos/DiscordBot/src/DiscordBot/
                BUILD_ID=dontKillMe
                screen -dmS "hal1320" dnx run &''' // ditto
                
            stage 'Post-Build'
            step([$class: 'GitHubCommitStatusSetter', statusResultSource: [$class: 'ConditionalStatusResultSource', results: []]])

        }
        
    }
    else
     {
        error 'NotImplemented'
    }
}