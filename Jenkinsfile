pipeline {
    agent any

    environment {
        IMAGE_NAME  = "securehub-backend"
        GITOPS_REPO = "https://github.com/DavidUyaguariJ/SecureHub-GitOps.git"
        GITOPS_BRANCH = "main"
    }

    stages {

        stage('Validate Branch') {
            steps {
                script {
                    def branch = env.BRANCH_NAME?.trim()
                    def isPR = env.CHANGE_ID != null
                    if (isPR) {
                        error "Los Pull Requests no ejecutan pipeline."
                    }
                    if (!(branch == 'develop' || branch == 'master' || branch.startsWith('stage'))) {
                        error "Branch no permitida: '${branch}'"
                    }
                    echo "Ejecutando pipeline para branch: '${branch}'"
                }
            }
        }

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Set Environment') {
            steps {
                script {
                    def branch = env.BRANCH_NAME?.trim()
                    echo "Branch detectada: '${branch}'"

                    if (branch == 'develop') {
                        env.BUILD_ENV         = 'Development'
                        env.KUBE_NAMESPACE    = 'dev'
                        env.IMAGE_TAG         = "dev-${env.BUILD_NUMBER}"
                        env.MANIFEST_PATH     = 'kubernetes/dev'
                        env.APPSETTINGS_ENV   = 'Develop'
                    } else if (branch?.startsWith('stage')) {
                        env.BUILD_ENV         = 'Staging'
                        env.KUBE_NAMESPACE    = 'stage'
                        env.IMAGE_TAG         = "stage-${env.BUILD_NUMBER}"
                        env.MANIFEST_PATH     = 'kubernetes/stage'
                        env.APPSETTINGS_ENV   = 'Stage'
                    } else if (branch == 'master') {
                        env.BUILD_ENV         = 'Production'
                        env.KUBE_NAMESPACE    = 'prod'
                        env.MANIFEST_PATH     = 'kubernetes/prod'
                        env.APPSETTINGS_ENV   = 'Production'
                        env.IMAGE_TAG         = powershell(
                            script: '''
                                $xml = [xml](Get-Content "SecureHub-Backend.csproj")
                                $xml.Project.PropertyGroup.Version
                            ''',
                            returnStdout: true
                        ).trim()
                    } else {
                        error "Branch no manejada: '${branch}'"
                    }

                    echo "BUILD_ENV:      ${env.BUILD_ENV}"
                    echo "KUBE_NAMESPACE: ${env.KUBE_NAMESPACE}"
                    echo "IMAGE_TAG:      ${env.IMAGE_TAG}"
                    echo "MANIFEST_PATH:  ${env.MANIFEST_PATH}"
                }
            }
        }

        stage('Build Docker Image') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'dockerhub-creds',
                    usernameVariable: 'DOCKER_USER',
                    passwordVariable: 'DOCKER_PASS'
                )]) {
                    script {
                        def buildEnv   = env.BUILD_ENV  ?: 'Development'
                        def imageTag   = env.IMAGE_TAG  ?: "dev-${env.BUILD_NUMBER}"
                        def dockerUser = env.DOCKER_USER
                        def imageName  = env.IMAGE_NAME
                        env.IMAGE_TAG  = imageTag

                        powershell """
                            docker build `
                                --build-arg BUILD_CONFIGURATION=Release `
                                --build-arg ASPNETCORE_ENVIRONMENT=${buildEnv} `
                                -t ${dockerUser}/${imageName}:${imageTag} `
                                -f Dockerfile .
                        """
                    }
                }
            }
        }

        stage('Login DockerHub') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'dockerhub-creds',
                    usernameVariable: 'DOCKER_USER',
                    passwordVariable: 'DOCKER_PASS'
                )]) {
                    bat 'echo %DOCKER_PASS%| docker login -u %DOCKER_USER% --password-stdin'
                }
            }
        }

        stage('Push Image') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'dockerhub-creds',
                    usernameVariable: 'DOCKER_USER',
                    passwordVariable: 'DOCKER_PASS'
                )]) {
                    script {
                        def imageTag   = env.IMAGE_TAG ?: "dev-${env.BUILD_NUMBER}"
                        def dockerUser = env.DOCKER_USER
                        def imageName  = env.IMAGE_NAME
                        powershell """
                            docker push ${dockerUser}/${imageName}:${imageTag}
                        """
                    }
                }
            }
        }

        stage('Tag Git (solo master)') {
            when { branch 'master' }
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'github-creds',
                    usernameVariable: 'GIT_USER',
                    passwordVariable: 'GIT_TOKEN'
                )]) {
                    script {
                        def tag      = env.IMAGE_TAG
                        def gitUser  = env.GIT_USER
                        def gitToken = env.GIT_TOKEN
                        powershell """
                            git config user.name "jenkins"
                            git config user.email "jenkins@local"
                            git tag "v${tag}"
                            git push https://${gitUser}:${gitToken}@github.com/DavidUyaguariJ/SecureHub-Backend.git "v${tag}"
                        """
                    }
                }
            }
        }

        stage('Checkout GitOps Repo') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'github-creds',
                    usernameVariable: 'GIT_USER',
                    passwordVariable: 'GIT_TOKEN'
                )]) {
                    script {
                        def gitopsBranch = env.GITOPS_BRANCH
                        bat """
                            if exist gitops-repo rmdir /s /q gitops-repo
                            git clone https://%GIT_USER%:%GIT_TOKEN%@github.com/DavidUyaguariJ/SecureHub-GitOps.git gitops-repo
                            cd gitops-repo && git checkout ${gitopsBranch}
                        """
                    }
                }
            }
        }

        stage('Update Manifests for ArgoCD') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'dockerhub-creds',
                    usernameVariable: 'DOCKER_USER',
                    passwordVariable: 'DOCKER_PASS'
                )]) {
                    script {
                        def namespace    = env.KUBE_NAMESPACE ?: 'dev'
                        def imageTag     = env.IMAGE_TAG      ?: "dev-${env.BUILD_NUMBER}"
                        def dockerUser   = env.DOCKER_USER    ?: 'daviduyaguarij'
                        def imageName    = env.IMAGE_NAME     ?: 'securehub-backend'
                        def manifestPath = env.MANIFEST_PATH  ?: 'kubernetes/dev'
                        def buildEnv     = env.BUILD_ENV      ?: 'Development'

                        def template   = readFile('deployment-template.yaml')
                        def deployment = template
                            .replace('${NAMESPACE}',            namespace)
                            .replace('${IMAGE_TAG}',            imageTag)
                            .replace('${DOCKERHUB_USER}',       dockerUser)
                            .replace('${IMAGE_NAME}',           imageName)
                            .replace('${ASPNETCORE_ENVIRONMENT}', buildEnv)

                        powershell """
                            New-Item -ItemType Directory -Force -Path "gitops-repo/${manifestPath}"
                        """
                        writeFile(
                            file: "gitops-repo/${manifestPath}/deployment-backend.yaml",
                            text: deployment
                        )
                        echo "Manifiesto actualizado en: gitops-repo/${manifestPath}/deployment-backend.yaml"
                    }
                }
            }
        }

        stage('Commit and Push to GitOps Repo') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'github-creds',
                    usernameVariable: 'GIT_USER',
                    passwordVariable: 'GIT_TOKEN'
                )]) {
                    script {
                        def namespace    = env.KUBE_NAMESPACE ?: 'dev'
                        def imageTag     = env.IMAGE_TAG      ?: "dev-${env.BUILD_NUMBER}"
                        def imageName    = env.IMAGE_NAME
                        def manifestPath = env.MANIFEST_PATH  ?: 'kubernetes/dev'
                        def gitopsBranch = env.GITOPS_BRANCH

                        def status = bat(
                            script: 'cd gitops-repo && git status --porcelain',
                            returnStdout: true
                        ).trim()

                        if (status) {
                            bat """
                                cd gitops-repo
                                git config user.name "Jenkins CI"
                                git config user.email "jenkins@securehub.local"
                                git add ${manifestPath}/deployment-backend.yaml
                                git commit -m "Update backend image to ${imageName}:${imageTag} for ${namespace} [skip ci]"
                                git push https://%GIT_USER%:%GIT_TOKEN%@github.com/DavidUyaguariJ/SecureHub-GitOps.git ${gitopsBranch}
                            """
                            echo "Cambios pusheados al repositorio GitOps"
                        } else {
                            echo "No hay cambios para commitear"
                        }
                        echo "ArgoCD sincronizará automáticamente"
                    }
                }
            }
        }
    }

    post {
        success {
            script {
                echo """
                    Pipeline completado exitosamente
                    Imagen: ${env.IMAGE_NAME}:${env.IMAGE_TAG}
                    Ambiente: ${env.BUILD_ENV}
                    Namespace: ${env.KUBE_NAMESPACE}
                    Manifiestos: ${env.MANIFEST_PATH}/deployment-backend.yaml
                """
            }
        }
        failure {
            echo "Pipeline falló. Revisa los logs para más detalles."
        }
    }
}