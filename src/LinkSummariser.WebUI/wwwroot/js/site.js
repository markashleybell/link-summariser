'use strict';

const connection = new signalR.HubConnectionBuilder().withUrl('/processorhub').build();

const markdownTemplate = document.getElementById('markdown-template').innerHTML;
const htmlTemplate = document.getElementById('html-template').innerHTML;

const links = document.getElementById('Links');
const submitButton = document.getElementById('submit-button');
const progress = document.getElementById('progress');

const markdownResultsDisplay = document.getElementById('markdown-results-display');
const htmlResultsDisplay = document.getElementById('html-results-display');

// Disable the send button until connection is established.
submitButton.disabled = true;

connection.on('Update', function (message) {
    progress.innerText = message;
});

connection.on('Errors', function (messages) {
    messages.forEach(m => console.error(m));
});

connection.on('Results', function (results) {
    markdownResultsDisplay.innerText = Mustache.render(markdownTemplate, results);
    htmlResultsDisplay.innerText = Mustache.render(htmlTemplate, results);
});

connection.start().then(function () {
    submitButton.disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

submitButton.addEventListener('click', function (event) {
    connection.invoke('Process', links.value).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

new ClipboardJS('.btn-copy');
