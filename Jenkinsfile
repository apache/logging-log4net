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
	}
	agent { label 'Windows' }
	tools {
		maven 'Maven 3.3.9 (Windows)'
		jdk 'JDK 1.8 (latest)'
	}
	environment {
		// TODO: find a better way to determine nant installation path
		NAnt = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
	}
	stages {
		stage('Checkout') {
			steps {
				checkout scm
			}
		}
		stage('Build net-4.0') {
			steps {
				bat "${NAnt} -buildfile:log4net.build compile-net-4.0"
			}
		}
		stage('Build mono-2.0') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-2.0'
					args '-v /etc/localtime:/etc/localtime:ro'
				}
			}
			steps {
				sh "nant -buildfile:log4net.build compile-mono-2.0"
			}
		}
		stage('Build mono-3.5') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-3.5'
					args '-v /etc/localtime:/etc/localtime:ro'
				}
			}
			steps {
				sh "nant -buildfile:log4net.build compile-mono-3.5"
			}
		}
		stage('Build mono-4.0') {
			agent {
				dockerfile {
					dir 'buildtools/docker/builder-mono-4.0'
					args '-v /etc/localtime:/etc/localtime:ro'
				}
			}
			steps {
				sh "nant -buildfile:log4net.build compile-mono-4.0"
			}
		}
		stage('Test on Windows') {
			steps {
				bat "${NAnt} -buildfile:tests\\nant.build"
			}
		}
		stage('Build-Site') {
			steps {
				bat "${NAnt} -buildfile:log4net.build generate-site"
			}
		}
		stage('Deploy-Site') {
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

