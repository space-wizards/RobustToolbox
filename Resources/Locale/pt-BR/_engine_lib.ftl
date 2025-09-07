# Used internally by the THE() function.
zzzz-the = { PROPER($gender) ->
    *[false] a { $gender }
     [true] { $gender }
    }

# Used internally by the SUBJECT() function.
zzzz-subject-pronoun = { $gender ->
    [male] ele
    [female] ela
    [epicene] eles
   *[neuter] ele
   }

# Used internally by the OBJECT() function.
zzzz-object-pronoun = { $gender ->
    [male] ele
    [female] ela
    [epicene] eles
   *[neuter] isso
   }

# Used internally by the POSS-PRONOUN() function.
zzzz-possessive-pronoun = { $gender ->
    [male] dele
    [female] dela
    [epicene] deles
   *[neuter] dele
   }

# Used internally by the POSS-ADJ() function.
zzzz-possessive-adjective = { $gender ->
    [male] dele
    [female] dela
    [epicene] deles
   *[neuter] dele
   }

# Used internally by the REFLEXIVE() function.
zzzz-reflexive-pronoun = { $gender ->
    [male] ele mesmo
    [female] ela mesmo
    [epicene] eles mesmos
   *[neuter] ele mesmo
   }

# Used internally by the CONJUGATE-BE() function.
zzzz-conjugate-be = { $gender ->
    [epicene] é
   *[other] é
   }

# Used internally by the CONJUGATE-HAVE() function.
zzzz-conjugate-have = { $gender ->
    [epicene] tenho
   *[other] tem
   }

# Used internally by the CONJUGATE-BASIC() function.
zzzz-conjugate-basic = { $gender ->
    [epicene] { $first }
   *[other] { $second }
   }
