        "use strict";

        window.SpeechRecognition = window.SpeechRecognition ||
                window.webkitSpeechRecognition ||
                null;

        class Integration {
            constructor() {



            }

            restartSession() {
                $.get("localhost:8080/reset/")
            }
        }

        class Conversation {
            constructor() {

                this.transcription = document.getElementById('transcription');
                this.logbox = document.getElementById('log');

                this.integration = new Integration();

            }

            record(speaker, transcript) {
                $.get("http://localhost:8080/say/?text="+encodeURI(transcript), function (data, status) {
                    document.getElementById('transcription').textContent = document.getElementById('transcription').textContent + "\n" + "BOT: " + data;
                    document.getElementById('transcription').scrollTop = document.getElementById('transcription').scrollHeight;
                    var u = new SpeechSynthesisUtterance();
                    u.text = data;
                    u.lang = 'en-US';
                    u.rate = 1.2;
                    speechSynthesis.speak(u);
                });
                this.transcription.textContent = this.transcription.textContent + "\n" + "USER: " + transcript;
                document.getElementById('transcription').scrollTop = document.getElementById('transcription').scrollHeight;
            }

            log(message) {
                this.logbox.innerHTML = message + '<br/>' + log.innerHTML;
            }

            beginConversation() {
                startRecording();
            }
        }

        var globalRecognizer = null;

        var conversation = new Conversation();

        function startRecording() {
            globalRecognizer = new window.SpeechRecognition();
            globalRecognizer.continuous = true;
            globalRecognizer.onresult = function (event) {
                for (var i = event.resultIndex; i < event.results.length; i++) {
                    conversation.record("Human Scum", event.results[i][0].transcript);
                }
                console.log("Result got!");
                globalRecognizer.stop();
                startRecording();
            };
            globalRecognizer.onsoundstart = function () {
                console.log("started!");
            }
            globalRecognizer.onsoundend = function () {
                console.log("ended!");
            }
            globalRecognizer.onerror = function (event) {
                conversation.log('Recognition error: ' + event.message);
            };
            globalRecognizer.start();
        }