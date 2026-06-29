 = Get-Content -Path index.vue -Raw;  =  -replace 'await fetchApprovers(row.policyid);', 'await fetchViewApprovers(row.policyid);'; Set-Content -Path index.vue -Value 
