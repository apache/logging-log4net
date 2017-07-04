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
		buildDiscarder(logRotator(numToKeepStr: '3'))
		skipDefaultCheckout()
	}
	agent {
		label 'ubuntu'
	}
	stages {
		stage('checkout') {
			steps {
				deleteDir()
				checkout scm
			}
		}
		def builds = [:]
		builds[0] = stage('build net-3.5') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -t:net-3.5 -buildfile:log4net.build compile-net-3.5"
				stash includes: 'bin/**/*.*', name: 'net-3.5-assemblies'
			}
		}
		builds[1] = stage('build net-3.5-cp') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -t:net-3.5 -buildfile:log4net.build compile-net-3.5-cp"
				stash includes: 'bin/**/*.*', name: 'net-3.5-cp-assemblies'
			}
		}
		builds[2] = stage('build net-4.0') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -buildfile:log4net.build compile-net-4.0"
				stash includes: 'bin/**/*.*', name: 'net-4.0-assemblies'
			}
		}
		builds[3] = stage('build net-4.0-cp') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -buildfile:log4net.build compile-net-4.0-cp"
				stash includes: 'bin/**/*.*', name: 'net-4.0-cp-assemblies'
			}
		}
		builds[4] = stage('build net-4.5') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -buildfile:log4net.build compile-net-4.5"
				stash includes: 'bin/**/*.*', name: 'net-4.5-assemblies'
			}
		}
		builds[5] = stage('build mono-2.0') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-2.0'
					args '-v /etc/localtime:/etc/localtime:ro'
					reuseNode true
				}
			}
			steps {
				checkout scm
				sh "nant -t:mono-2.0 -buildfile:log4net.build compile-mono-2.0"
				stash includes: 'bin/**/*.*', name: 'mono-2.0-assemblies'
			}
		}
		builds[6] = stage('build mono-3.5') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-3.5'
					args '-v /etc/localtime:/etc/localtime:ro'
					reuseNode true
				}
			}
			steps {
				checkout scm
				sh "nant -t:mono-3.5 -buildfile:log4net.build compile-mono-3.5"
				stash includes: 'bin/**/*.*', name: 'mono-3.5-assemblies'
			}
		}
		builds[7] = stage('build mono-4.0') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-4.0'
					args '-v /etc/localtime:/etc/localtime:ro'
					reuseNode true
				}
			}
			steps {
				checkout scm
				sh "nant -t:mono-4.0 -buildfile:log4net.build compile-mono-4.0"
				stash includes: 'bin/**/*.*', name: 'mono-4.0-assemblies'
			}
		}
		builds[8] = stage('build netstandard') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-netstandard'
					reuseNode true
				}
			}
			steps {
				checkout scm
				
				// workaround: https://github.com/NuGet/Home/issues/5106
				sh 'export HOME=/home'
				
				// compile 
				sh 'nant compile-netstandard'
				stash includes: 'bin/**/*.*', name: 'netstandard-assemblies'
			}
		}
		builds[9] = stage('build site') {
			agent { label 'Windows' }
			tools {
				maven 'Maven 3.3.9 (Windows)'
				jdk 'JDK 1.8 (latest)'
			}
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -buildfile:log4net.build generate-site"
				stash includes: 'target/site/**/*.*', name: 'site'
			}
		}

		// run all builds in parallel
		parallel builds

		// TODO: testing needs to be refactored
		stage('test net-4.0') {
			agent { label 'Windows' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				unstash 'net-4.0-assemblies'
				bat "${NANT_BIN} -buildfile:tests\\nant.build"
				// TODO: stash test results
			}
		}

		// prepare package
		stage('prepare package') {
			steps {
				// assemble package by unstashing components
				dir('package') {
					unstash 'net-3.5-assemblies'
					unstash 'net-3.5-cp-assemblies'
					unstash 'net-4.0-assemblies'
					unstash 'net-4.0-cp-assemblies'
					unstash 'net-4.5-assemblies'
					unstash 'mono-2.0-assemblies'
					unstash 'mono-3.5-assemblies'
					unstash 'mono-4.0-assemblies'
					unstash 'netstandard-assemblies'
					unstash 'site'
				}
				
				// move site
				sh 'mv package/target/site/ package/site/'
				sh 'rmdir -p --ignore-fail-on-non-empty package/target'
				
				// archive package
				archive 'package/**/*.*'
			}
		}
		stage('publish site') {
			when {
				branch 'master'
			}
			steps {
				echo 'This is a placeholder for the deployment of the site'
			}
		}
	}
	post {
		failure {
			step([$class: 'Mailer', notifyEveryUnstableBuild: false, recipients: 'dev@logging.apache.org'])
		}
	}
}

