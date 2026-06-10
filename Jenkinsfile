@Library('securehub-lib') _

securehubPipeline(
    service:       'backend',
    imageName:     'securehub-backend',
    devEnv:        'Development',
    stageEnv:      'Staging',
    prodEnv:       'Production',
    repoUrl:       'github.com/DavidUyaguariJ/SecureHub-Backend.git',
    getVersionCmd: {
        def csproj = powershell(
            returnStdout: true,
            script: '''
                $f = Get-ChildItem -Recurse -Filter "*.csproj" | 
                     Where-Object { $_.Name -notmatch "Test" } | 
                     Select-Object -First 1 -ExpandProperty FullName
                $xml = [xml](Get-Content $f)
                $xml.Project.PropertyGroup.Version
            '''
        ).trim()
        return csproj ?: "prod-${env.BUILD_NUMBER}"
    }
)