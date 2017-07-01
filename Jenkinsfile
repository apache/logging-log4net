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
	environment {
		NAnt = 'F:\\jenkins\\tools\\nant\\nant-0.92\\bin\\NAnt.exe'
	}
	stages {
		// TODO: find a better way to determine nant latest
		stage('Checkout') {
			steps {
				checkout scm
			}
		}
		stage('Build') {
			steps {
				bat "${NAnt} -buildfile:log4net.build"
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
			echo 'Failed build'
			// TODO: send email as soon as the entire building is more stable
			//step([$class: 'Mailer', notifyEveryUnstableBuild: true, recipients: 'dev@logging.apache.org'])
		}
	}
}

