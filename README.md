# FetchRewards

Hey, so heres my solution. First off, its commented a ton. I dont necessarily like  to overly comment things, I think code should just be more readable and comments should be special, but its how ive been they've wanted it at my current job the past two years, and the test said to assume whoever reads this knows nothing about anything, so I let it be. Also, I know it isnt timed, but i took a little while because i havent touched ASP in a little under a year now and I had to quickly get up to speed on it again.

This is a project built from an ASP.Net Core Web API template. It is in C#. It uses Entity Framwork. It has Swagger enabled and set up to check on and mess about with the endpoints. Upon starting the solution, you'll be brought to the Swagger page which will allow you to send data to and get responses from the various endpoints.

Because the code is commented how it is, I'll try to just run through the various files and functions and give a hopefully brief explanation.


# Code Stuff:
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

Now that we have taken old point deducutions into account, we can actually spend the points. we iterate through the transactions and save the amount of points to be deducted. we run SubtractPoints for each TransactionRecord and it will return the remainder(if any). This remainder is then subtracted from old amount giving us the amount of points subtracted from the TransactionRecord. This data is then saved in the TransactionsCompleted list so we know what work was done.  We then keep iterating thorugh the transactiion records and doing this until we have deducted the required amount of points.

Once that's done, we run ChangeTracker.Clear(). Because entity framework keeps track of these objects, even when we abstract them out into lists, all the changes and point deductions will be reflected in the database unless we do that.

Once that's done we just add the items in the TransactionsCompleted list to the transactionRecord, and return a list of SpendResult objects so it can be used to send out JSON response.

### CalculatePointBalance
This function gets the point balances for the various payers. We get all trasactions, group them by payer, and then go throught he groups and do math. we take each Point balance, add it to a TransactionBalanceResult object created specifically for this purpose. If, while iterating, the Payer name changes, we know we've finished the previous group of payers and we save that result. Then we start a new TransactionbalanceResult object and continue. once that's done, we have a list of TransactionBalanceResults to be turned into a JSON reponse

### CalculatePointBalanceWithLinq
This is originally how i did it with Linq. It's explained better in the comments, but it felt like cheating because its literally one line and a return. So I did the previous method and thats the one that gets called. But here's how it would look in Linq if you're curious. Make groups, sum the groups, and return the results as TransactionBalanceResult objects.

### SubtractPointsByTransactionRecord
This lets us take a transaction with a negative balance and the list of transaction records, subtract points and modify the values of those TransactionRecords, and then return the modified list, so that our spend operation will take the right amount of points from the right places.

This first line is Linq, it looks like a lot, but it just says we want the right payer, we want only older transaction records, which have a positive Points value, and then order them by time.

We run a quick check to make sure there are any records at all, then we run a check to make sure that we have enough points for a given payer, overall, to deduct. 

We then iterate thorugh the transaction records, we deduct points, the SubtractPoints function will return the remainder to be subtracted(if there is any), and we keep going through records and subtracting points until there is no remainder, which means our operation has finished. 

We then return the new modified list of transactions with the points deducted.

### SpendResult and TransactionBalanceResult
Making these objects just makes it easier to send the results back as JSON. They just hold payer and points data. Theyre pretty similar and could probably be smashed into a single PointsResult class, but they have slightly different functionality and I'd have to reconcile that. I figured this would be fine for this test.


# Running the thing:
Install Visual Studio.

Pull the solution from github. 

Open *'FetchRewards.sln'* with Visual Studio.

Click the play button titled *'FetchRewards'* on the home ribbon of Visual Studio.

It should automatically open a browser window, and open it to *'https://localhost:xxxx/swagger/index.html'*

(also you might need to trust the certificate, there might be a dialog box about that)

The service is now running!! Hopefully. 
If so, you should be able to send POST data to the endpoints as JSON, just like the test examples are formatted, or you can use Swagger.

### Swagger
If using Swagger, dropdown the endpoint you want to work with, click the *'Try It Out'* button, and then give it JSON data and click execute. 
For SpendPoints it just wants a number value, and for GET requests just click *'execute'*. 





____________________
____________________
# Some Extra Rambling<sub>(optional)</sub>
So this was really cool. This is probably the best time I've had applying for a job, to be honest. I love that I get to use whatever stack I want and Im not under time constraints. I love that I was given an actual problem that requires critical thinking and decision making, as opposed to multiple-choice questions. I love that it was Fetch Rewards related and not a random thing. 

I was starting to get rusty on ASP stuff, and I've never tried to build out a little web service from scratch before, so I did learn some things which will make this beneficial either way. So, like I said, overall i really loved this exercise. You guys are awesome for this, if I ever get into a position to handle a hiring process I'm doing it this way. 

About this solution, so, originally I had made another property in the TransactionRecord object, 'PointsRemaining', which would be tracked in the database and subtracted from. So, in order to not have to pull all transaction records *every time*, and not have to recalculate things *every time*, I could just say "get records with points remaining". Once the points were spent, the value would be set at 0 and the item would be left out the next time i called the db. 

This worked fine at first because I had taken your JSON calls and ordred them by time and sent them in order. Because some of the calls are point deductions, when submitting a transaction record they would deduct points from the oldest possible record at the point of submission. Then, when I did them in the order provided, and a new record came in whose time was was older than that, it would be messed up and not have points subtracted. 

So, while I do hate having to grab *all* the records and do *all* the math on them every single time(which doesn't at all feel optimal), theres no telling when a transaction record with an older timestamp would come in and mess all that up. So this is how I ended up doing it. I assume it was part of the challenge that the data doesnt necessarily arrive in order based on time, but it really threw me off when I went back to the order provided in the test and I had to change some things really quickly. 

Also, there maybe could be something to be done here about stored procedures and things of that nature, but I dont think that's what you wanted, and I'm not actually working with a database with which to make any stored procedures.

Well, that's all i have. I can get really nervous on the phone, and especially with new people, and about stuff like this where its a job search thing. But thats another thing I like like about this. At least this way I dont really have to worry about all that. I mean, if you're reading this right now, that's kind of exactly what I mean. I can just do work, and explain myself, and show my code first instead of seizing up in the moment. Which, i still might do, but at least it'll happen second. 
