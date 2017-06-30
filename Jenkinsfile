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
	/*
	TODO: eventually enable timeouts for the entire pipeline
	options {
		timeout(time: 1, unit 'HOURS')
	}*/
	stages {
		// TODO: find a better way to determine nant latest
		def NANT_LATEST="F:\\jenkins\\tools\\nant\\nant-0.92\\bin"
		stage('Checkout') {
			agent { label 'Windows' }
			steps {
				checkout scm
			}
		}
		stage('Build') {
			agent { label 'Windows' }
			steps {
				withEnv(["Path+NANT=$NANT_LATEST"]) {
					bat "NAnt.exe -buildfile:log4net.build"
				}
			}
		}
		stage('Test') {
			agent { label 'Windows' }
			steps {
				withEnv(["Path+NANT=$NANT_LATEST"]) {
					bat "NAnt.exe -buildfile:tests\\nant.build"
				}
			}
		}
		stage('Build-Site') {
			agent { label 'ubuntu' }
			steps {
				echo 'Ths is a placeholder for the build of the site'
			}
		}
		stage('Deploy-Site') {
			agent { label 'ubuntu' }
			when {
				branch 'master'
			}
			steps {
				echo 'This is a placeholder for the deployment of the site'
			}
		}
	}
	post {
		failed {
			echo 'Failed build'
			// TODO: send email as soon as the entire building is more stable
			//step([$class: 'Mailer', notifyEveryUnstableBuild: true, recipients: 'dev@logging.apache.org'])
		}
	}
}

