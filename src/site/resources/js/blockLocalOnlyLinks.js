/*
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.

*/
function getElementsByClass(node,searchClass,tag) {
    var classElements = new Array();
    var els = node.getElementsByTagName(tag);
    var elsLen = els.length;
    var pattern = new RegExp("\\b"+searchClass+"\\b");
    for (i = 0, j = 0; i < elsLen; i++) {
        if ( pattern.test(els[i].className) ) {
            classElements[j] = els[i];
            j++;
        }
    }
    return classElements;
}
function BlockLocalOnlyLinks() {
    if (window.location.protocol!="file:") {
        var links = getElementsByClass(document, "localOnly", "a");
        for(var i=0; i < links.length; i++) {
            links[i].href = "#overview";
        }
    }
}
BlockLocalOnlyLinks();
