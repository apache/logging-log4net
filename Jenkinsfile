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
		timeout(time: 4, unit: 'HOURS')
		buildDiscarder(logRotator(numToKeepStr: '3'))
		skipDefaultCheckout()
		disableConcurrentBuilds()
	}
	agent {
		label 'ubuntu&&!H26'
	}
	stages {
		// prepare node for builds
		stage('checkout') {
			steps {
				deleteDir()
				checkout scm
			}
		}

		// builds
		stage('build netstandard-1.3') {
			steps {
				script {
					checkout scm
					def builder_dir = "buildtools/docker/builder-netstandard-1.3"

					// calculate args required to build the docker container
					def JENKINS_UID = sh (
						script: "stat -c \"%u\" $builder_dir",
						returnStdout: true
					).trim()
					def JENKINS_GID = sh (
						script: "stat -c \"%g\" $builder_dir",
						returnStdout: true
					).trim()

					// build docker container
					def builder = docker.build 'builder-netstandard-1.3:latest', "--file $builder_dir/Dockerfile --build-arg JENKINS_UID=$JENKINS_UID --build-arg JENKINS_GID=$JENKINS_GID $builder_dir"

					// run docker container
					builder.inside {
						// compile
						sh "nant compile-netstandard-1.3"
						stash includes: 'bin/**/*.*', name: 'netstandard-1.3-assemblies'

						// test
						sh "nant -buildfile:tests/nant.build runtests-netstandard-1.3"
						stash includes: 'tests/bin/**/*.trx', name: 'netstandard-1.3-testresults', allowEmpty: true
						stash includes: 'tests/bin/**/*.log', name: 'netstandard-1.3-testlogs', allowEmpty: true
					}
				}
			}
		}
		stage('build net-2.0') {
			agent { label 'windows-2012-1||windows-2012-2||windows-2012-3' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -t:net-2.0 -buildfile:log4net.build compile-net-2.0"
				stash includes: 'bin/**/*.*', name: 'net-2.0-assemblies'
				bat "${NANT_BIN} -t:net-2.0 -buildfile:tests/nant.build runtests-net-2.0"
				stash includes: 'tests/bin/**/*.nunit.xml', name: 'net-2.0-testresults'
			}
		}
		stage('build net-3.5') {
			agent { label 'windows-2012-1||windows-2012-2||windows-2012-3' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -t:net-3.5 -buildfile:log4net.build compile-net-3.5"
				stash includes: 'bin/**/*.*', name: 'net-3.5-assemblies'
				bat "${NANT_BIN} -t:net-3.5 -buildfile:tests/nant.build runtests-net-3.5"
				stash includes: 'tests/bin/**/*.nunit.xml', name: 'net-3.5-testresults'
			}
		}
		stage('build net-3.5-cp') {
			agent { label 'windows-2012-1||windows-2012-2||windows-2012-3' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -t:net-3.5 -buildfile:log4net.build compile-net-3.5-cp"
				stash includes: 'bin/**/*.*', name: 'net-3.5-cp-assemblies'
				bat "${NANT_BIN} -t:net-3.5 -buildfile:tests/nant.build runtests-net-3.5-cp"
				stash includes: 'tests/bin/**/*.nunit.xml', name: 'net-3.5-cp-testresults'
			}
		}
		stage('build net-4.0') {
			agent { label 'windows-2012-1||windows-2012-2||windows-2012-3' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -t:net-4.0 -buildfile:log4net.build compile-net-4.0"
				stash includes: 'bin/**/*.*', name: 'net-4.0-assemblies'
				bat "${NANT_BIN} -t:net-4.0 -buildfile:tests/nant.build runtests-net-4.0"
				stash includes: 'tests/bin/**/*.nunit.xml', name: 'net-4.0-testresults'
			}
		}
		stage('build net-4.0-cp') {
			agent { label 'windows-2012-1||windows-2012-2||windows-2012-3' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -t:net-4.0 -buildfile:log4net.build compile-net-4.0-cp"
				stash includes: 'bin/**/*.*', name: 'net-4.0-cp-assemblies'
				bat "${NANT_BIN} -t:net-4.0 -buildfile:tests/nant.build runtests-net-4.0-cp"
				stash includes: 'tests/bin/**/*.nunit.xml', name: 'net-4.0-cp-testresults'
			}
		}
		stage('build net-4.5') {
			agent { label 'windows-2012-1||windows-2012-2||windows-2012-3' }
			environment {
				NANT_BIN = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
			}
			steps {
				deleteDir()
				checkout scm
				bat "${NANT_BIN} -t:net-4.0 -buildfile:log4net.build compile-net-4.5"
				stash includes: 'bin/**/*.*', name: 'net-4.5-assemblies'
				bat "${NANT_BIN} -t:net-4.0 -buildfile:tests/nant.build runtests-net-4.5"
				stash includes: 'tests/bin/**/*.nunit.xml', name: 'net-4.5-testresults'
			}
		}
		stage('build mono-2.0') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-2.0'
					args '-v /etc/localtime:/etc/localtime:ro'
					reuseNode true
				}
			}
			steps {
				sh "rm -rf bin/ tests/"
				checkout scm
				sh "nant -t:mono-2.0 -buildfile:log4net.build compile-mono-2.0"
				stash includes: 'bin/**/*.*', name: 'mono-2.0-assemblies'
				sh "nant -t:mono-2.0 -buildfile:tests/nant.build runtests-mono-2.0"
				stash includes: 'tests/bin/**/*.nunit.xml', name: 'mono-2.0-testresults'
			}
		}
		stage('build mono-3.5') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-3.5'
					args '-v /etc/localtime:/etc/localtime:ro'
					reuseNode true
				}
			}
			steps {
				sh "rm -rf bin/ tests/"
				checkout scm
				sh "nant -t:mono-3.5 -buildfile:log4net.build compile-mono-3.5"
				stash includes: 'bin/**/*.*', name: 'mono-3.5-assemblies'
				sh "nant -t:mono-3.5 -buildfile:tests/nant.build runtests-mono-3.5"
				stash includes: 'tests/bin/**/*.nunit.xml', name: 'mono-3.5-testresults'
			}
		}
		stage('build mono-4.0') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-4.0'
					args '-v /etc/localtime:/etc/localtime:ro'
					reuseNode true
				}
			}
			steps {
				sh "rm -rf bin/ tests/"
				checkout scm
				sh "nant -t:mono-4.0 -buildfile:log4net.build compile-mono-4.0"
				stash includes: 'bin/**/*.*', name: 'mono-4.0-assemblies'
				sh "nant -t:mono-4.0 -buildfile:tests/nant.build runtests-mono-4.0"
				stash includes: 'tests/bin/**/*.nunit.xml', name: 'mono-4.0-testresults'
			}
		}
		stage('build site') {
			agent { label 'windows-2012-1||windows-2012-2||windows-2012-3' }
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

		// prepare package
		stage('prepare package') {
			steps {
				// assemble package by unstashing components
				dir('package') {
					// unstash assemblies
					unstash 'net-3.5-assemblies'
					unstash 'net-3.5-cp-assemblies'
					unstash 'net-4.0-assemblies'
					unstash 'net-4.0-cp-assemblies'
					unstash 'net-4.5-assemblies'
					unstash 'mono-2.0-assemblies'
					unstash 'mono-3.5-assemblies'
					unstash 'mono-4.0-assemblies'
					unstash 'netstandard-1.3-assemblies'

					// unstash test results
					unstash 'net-3.5-testresults'
					unstash 'net-3.5-cp-testresults'
					unstash 'net-4.0-testresults'
					unstash 'net-4.0-cp-testresults'
					unstash 'net-4.5-testresults'
					unstash 'mono-2.0-testresults'
					unstash 'mono-3.5-testresults'
					unstash 'mono-4.0-testresults'
					unstash 'netstandard-1.3-testresults'
					unstash 'netstandard-1.3-testlogs'

					// unstash site
					unstash 'site'
				}

				// move site
				sh 'mv package/target/site/ package/site/'
				sh 'rmdir -p --ignore-fail-on-non-empty package/target'

				// record git status into the package
				sh 'git log -1 > package/git.commit'

				// archive package
				archive 'package/**/*.*'
			}
		}

		// archive the tests (this also checks if tests failed; if that's the case this stage should fail)
		stage('check test results') {
			steps {
				// record test results
				step([
					$class        : 'XUnitBuilder',
					thresholds    : [
						[
							$class: 'FailedThreshold', unstableThreshold: '1'
						]
					],
					tools         : [
						[
							$class               : 'NUnitJunitHudsonTestType',
							deleteOutputFiles    : false,
							failIfNotNew         : true,
							pattern              : 'package/tests/bin/**/*.nunit.xml',
							skipNoTestFiles      : true,
							stopProcessingIfError: true
						],
						[
							$class               : 'MSTestJunitHudsonTestType',
							deleteOutputFiles    : false,
							failIfNotNew         : true,
							pattern              : 'package/**/*.trx',
							skipNoTestFiles      : true,
							stopProcessingIfError: true
						]
					]
				])
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
			// TODO: change this to dev@
			step([$class: 'Mailer', notifyEveryUnstableBuild: false, recipients: 'notifications@logging.apache.org'])
		}
	}
}
