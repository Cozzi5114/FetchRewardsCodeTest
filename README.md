# FetchRewards

Hey, so heres my solution. First off, its commented a ton. I dont necessarily like  to overly comment things, but its how ive been conditioned at my current job the past two years, and I was told to assume whoever reads this knows nothing about anything, so I let it be. Also, I know it isnt timed, but i took a little while because i havent touched ASP in about a year now and I had to quickly get up to speed on it again.

This is a project built from an ASP.Net Core Web API template. It is in C#. It uses Entity Framwork. It has Swagger enabled and set up to check on and mess about with the endpoints. Upon starting the solution, you'll be brought to the Swagger page which will allow you to send data to and get responses from the various endpoints.

Because the code is commented how it is, I'll try to just run through the various files and functions and give a hopefully brief explanation.



## FetchRewards/Models/TransactionRecord.cs
This class is the transaction records and the model that Entity Framework will use to create and access the database layer. It has an ID, along with the Payer, Points, Timestamp stuff that it requires. An empty constructor, a not empty constructor, and a SubtractPoints function. 

### SubtractPoints 
Subtracts points from this transaction record, and will return a positive integer of the remaining points if there were not enough points to do the subtraction. This way if it returns anything but 0, we know to continue going through the transaction record and subtracting points. I could have went deeper and made a 'Payer' model, a 'User', and had the transaction records link to those with a foreign key and all that, but I figured that was way too much for this. 


## FetchRewards/Models/TransactionRecordContext.cs
This can be ignored, it's a requirement for Entity Framework to scaffold the controllers.


## FetchRewards/Controllers/TransactionRecordsController.cs
This is the controller for the TransactionRecords endpoints. This is mostly generated with EntityFramework. It came with basic CRUD operations and I got rid of the unused ones. It has a function for getting all transaction records, as well as getting a single one by ID. 

It also has a function for submitting transaction records. Before submitting a transaction record, we have a check to see if it will cause a negative point balance for the given payer. If not, it is safe, and the record is submitted to the database.


## FetchRewards/Models/PointsController.cs
This is where most of the work happens. We can check the point balance for all payers, and spend points,

### GetPointBalance 
runs the CalculatePointsBalance function and sends the resulting point balances on as JSON.

### PostSpendPoints 
runs SpendPoints, which will spend the points, submit transaction records for the spend operation, and return what changes happened. It made more sense to me architecturally to have these two functions be dumb, and instead call other functions to do the work. Since they're endpoints thats all they will do.

### SpendPoints 
This is our actual logic. We get all records and order them by time. A quick check to make sure there even are any records. Then, another check to see if there are enough points, overall, to complete the spend operation. If so we continue.

We then run through all transactions, when we find a negative transaction(a point deduction), we run the SubtractPointsByTransactionRecord function. This function will iterate through and subtract points from the oldest possible transaction records until it has deducted the required amount of points. This will set the stage for us to actually complete our SpendPoints operation. 

Now that we have taken old point deducutions into account, we can actually spend the points. we iterate through the transactions and save the amount of points to be deducted. we run SubtractPoints for each transactionRecord and it will return the remainder(if any). This remainder is then subtracted from old amount giving us the amount of points subtracted from the TransactionRecord. this data is then saved in the TransactionCompleted list so we know what work was done.  We then keep iterating thorugh the transactiion records and doing this until we have deducted the required amount of points.

Once that's done, we run ChangeTracker.Clear(). Because entity framework keeps track of these objects, even when we abstract them out into lists, all the changes and point deductions will be reflected in the database unless we do that.

Once that's done we just add the items in the TransactionsCompleted list to the transactionRecord, and return a list of SpendResult objects so it can be used to send out JSON response.

### CalculatePointBalance
This function gets the point balances for the various payers. We get all trasactions, group them by payer, and then go throught he groups and do math. we take each Point balance, add it to a TransactionBalanceResult object created specifically for this purpose. If while iterating the payer name changes, we know we've finished the previous group of payers and we save that result and start a new TransactionbalanceResult object and continue. once that's done, we have a list of TransactionBalanceResults to be used as a JSON reponse

### CalculatePointBalanceWithLinq
This is originally how i did it with Linq. It's explained better int he comments, but it felt like cheating because its literally one line and a return. So I did the previous method and thats the one that gets called. But here's how it would look in Linq if you're curious. Make groups, sum the groups, and return the results as TransactionBalanceResult objects.

### SubtractPointsByTransactionRecord
This lets us take a transaction with a negative balance and the list of transaction records, subtract poitns and omdify the values of those records, and then return the modified list, so that our spend operation will take the right amount of points from the right places.

This first line is Linq, it a lot, but it just says we want the right payer, we want only older transaction records, which have a positive Points value(its not a record of a point deduction), and order them by time.

We run a quick check to make sure there are any records at all, then we run a check to make sure that we have enough points for a given payer, overall, to deduct. 

We then iterate thorugh the transaction records, we deduct points, the SubtractPoints function will return the remainder to be subtracted(if there is any), and we keep going through records and subtracting points until there is no remainder, which means our operation has finished. We then return the new modified list of transactions with the points deducted

### SpendResult and TransactionBalanceResult
Making these objects just makes it easier to send the results back as JSON. They just hold payer and points data. Theyre pretty similar and could probably be smashed into a single PointsResult class, but they have slightly different functionality and I'd have to reconcile that. I figured this would be fine for this test.


# Running the code
Install Visual Studio.
Pull the solution from github. 
Open *'FetchRewards.sln'* with Visual Studio.
Click the play button titled *'FetchRewards'* on the home ribbon of Visual Studio.
It should automatically open a browser window, and open it to *'https://localhost:xxxx/swagger/index.html'*
(also you might need to trust the certificate, there might be a dialog box about that)
The service is now running! You can send POST data to the endpoints as JSON, just like the test examples, or you can use Swagger.

If using Swagger, dropdown the endpoint you want to work with, click the *'Try It Out'* button, and then give it JSON data and click execute. 
For SpendPoints it just wants a number value, and for GET requests just click *'execute'*. 

