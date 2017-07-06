
function trackDocumentCount() {

    var collection = getContext().getCollection();

    var createdDoc = getContext().getRequest().getBody();

    var filterQuery = 'SELECT * FROM root r WHERE r.ismeta = true';

    var accept = collection.queryDocuments(collection.getSelfLink(), filterQuery,
        updateMetadataCallback);

    if(!accept) throw "Error while updating collection metadata";

    function updateMetadataCallback(err, docs, options) {

        if(err) throw err;

        if(!docs || docs.length == 0) {

            var metaDoc = {
                deviceId: createdDoc.deviceId,
                count: 1,
                ismeta: true
            };

            var accept = collection.createDocument(collection.getSelfLink(), metaDoc, function(err) {
                if (err) throw err;
            });

            if(!accept) throw "Error while creating collection metadata";
        } else {

            var metaDoc = docs[0];

            metaDoc.count += 1;

            var accept = collection.replaceDocument(metaDoc._self, metaDoc, function(err) {
                if(err) throw err;
            });

            if(!accept) throw "Error while updating collection metadata";
        }                                                                   
    }
}
