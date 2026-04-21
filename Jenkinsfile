@Library('securehub-lib') _

securehubPipeline(
    service:   'backend',
    imageName: 'securehub-backend',
    devEnv:    'Development',
    stageEnv:  'Staging',
    prodEnv:   'Production',
    repoUrl:   'github.com/DavidUyaguariJ/SecureHub-Backend.git',
    getVersionCmd: {
        powershell(
            script: '''
                $xml = [xml](Get-Content "SecureHub-Backend.csproj")
                $xml.Project.PropertyGroup.Version
            ''',
            returnStdout: true
        ).trim()
    }
)