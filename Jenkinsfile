/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

pipeline {
	options {
		timeout(time: 1, unit: 'HOURS')
		buildDiscarder(logRotator(numToKeepStr: '1'))
		skipDefaultCheckout()
	}
	agent {
		label 'ubuntu'
	}
	stages {
		stage('build net-4.0') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = getPathToNAntOnWindows()
			}
			steps {
				checkout scm
				bat "${NANT_BIN} -buildfile:log4net.build compile-net-4.0"
				stash includes: 'bin/**/*.*', name: 'net-4.0-assemblies'
			}
		}
		stage('build net-4.0-cp') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = getPathToNAntOnWindows()
			}
			steps {
				checkout scm
				bat "${NANT_BIN} -buildfile:log4net.build compile-net-4.0-cp"
				stash includes: 'bin/**/*.*', name: 'net-4.0-cp-assemblies'
			}
		}
		stage('build net-4.5') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = getPathToNAntOnWindows()
			}
			steps {
				checkout scm
				bat "${NANT_BIN} -buildfile:log4net.build compile-net-4.5"
				stash includes: 'bin/**/*.*', name: 'net-4.5-assemblies'
			}
		}
		stage('build mono-2.0') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-2.0'
					args '-v /etc/localtime:/etc/localtime:ro'
				}
			}
			steps {
				checkout scm
				sh "nant -t:mono-2.0 -buildfile:log4net.build compile-mono-2.0"
				stash includes: 'bin/**/*.*', name: 'mono-2.0-assemblies'
			}
		}
		stage('build mono-3.5') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-3.5'
					args '-v /etc/localtime:/etc/localtime:ro'
				}
			}
			steps {
				checkout scm
				sh "nant -t:mono-3.5 -buildfile:log4net.build compile-mono-3.5"
				stash includes: 'bin/**/*.*', name: 'mono-3.5-assemblies'
			}
		}
		stage('build mono-4.0') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-4.0'
					args '-v /etc/localtime:/etc/localtime:ro'
				}
			}
			steps {
				checkout scm
				sh "nant -t:mono-4.0 -buildfile:log4net.build compile-mono-4.0"
				stash includes: 'bin/**/*.*', name: 'mono-4.0-assemblies'
			}
		}
		stage('build site') {
			agent { label 'Windows' }
			tools {
				maven 'Maven 3.3.9 (Windows)'
				jdk 'JDK 1.8 (latest)'
			}
			environment {
				NANT_BIN = getPathToNAntOnWindows()
			}
			steps {
				checkout scm
				bat "${NANT_BIN} -buildfile:log4net.build generate-site"
				stash includes: 'target/site-deploy/**/*.*', name: 'site'
			}
		}
		// TODO: testing needs to be refactored
		stage('test on Windows') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = getPathToNAntOnWindows()
			}
			steps {
				checkout scm
				bat "${NANT_BIN} -buildfile:tests\\nant.build"
				// TODO: stash test results
			}
		}
		stage('prepare package') {
			steps {
				unstash 'net-4.0-assemblies'
				unstash 'net-4.0-cp-assemblies'
				unstash 'net-4.5-assemblies'
				unstash 'mono-2.0-assemblies'
				unstash 'mono-3.5-assemblies'
				unstash 'mono-4.0-assemblies'
				unstash 'site'
			}
		}
		stage('deploy site') {
			when {
				branch 'master'
			}
			steps {
				echo 'This is a placeholder for the deployment of the site'
			}
		}
	}
	post {
		always {
			archive '**/*.*'
		}
		failure {
			step([$class: 'Mailer', notifyEveryUnstableBuild: false, recipients: 'dev@logging.apache.org'])
		}
	}
}

// TODO: find a better way to determine nant installation path
def getPathToNAntOnWindows() {
	'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
}

